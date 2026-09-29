package com.saferide.bus.service;

import com.saferide.bus.constants.MessagingConstants;
import com.saferide.bus.constants.ResponseMessages;
import com.saferide.bus.dto.AssignDriverRequest;
import com.saferide.bus.dto.BusResponse;
import com.saferide.bus.dto.CreateBusRequest;
import com.saferide.bus.dto.PagedResult;
import com.saferide.bus.dto.UpdateBusRequest;
import com.saferide.bus.entity.Bus;
import com.saferide.bus.exception.AppException;
import com.saferide.bus.mapper.BusMapper;
import com.saferide.bus.messaging.BusCreated;
import com.saferide.bus.messaging.BusDriverAssigned;
import com.saferide.bus.messaging.BusStatusChanged;
import com.saferide.bus.messaging.RabbitEventPublisher;
import com.saferide.bus.projection.SchoolStatus;
import com.saferide.bus.projection.SchoolStatusRepository;
import com.saferide.bus.projection.SchoolStatuses;
import com.saferide.bus.repository.BusRepository;
import com.saferide.bus.repository.BusSpecifications;
import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneId;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;
import org.springframework.data.domain.Sort;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class BusService {

    private static final int MAX_PAGE_SIZE = 50;

    /** Must match Subscription.GraceDays in the School service. */
    private static final int GRACE_DAYS = 7;

    private final BusRepository busRepository;
    private final BusDocumentService busDocumentService;
    private final SchoolStatusRepository schoolStatusRepository;
    private final BusMapper busMapper;
    private final RabbitEventPublisher publisher;
    private final ZoneId zone;

    public BusService(
            BusRepository busRepository,
            BusDocumentService busDocumentService,
            SchoolStatusRepository schoolStatusRepository,
            BusMapper busMapper,
            RabbitEventPublisher publisher,
            @Value("${saferide.timezone:Asia/Kolkata}") String timezone) {
        this.busRepository = busRepository;
        this.busDocumentService = busDocumentService;
        this.schoolStatusRepository = schoolStatusRepository;
        this.busMapper = busMapper;
        this.publisher = publisher;
        this.zone = ZoneId.of(timezone);
    }

    @Transactional
    public BusResponse create(UUID schoolId, CreateBusRequest request) {
        requireApprovedSchool(schoolId);
        requireActiveSubscription(schoolId);
        requireBusCapacity(schoolId);

        String registration = Bus.normalizeRegistrationNumber(request.registrationNumber());
        if (busRepository.existsBySchoolIdAndRegistrationNumber(schoolId, registration)) {
            throw new AppException.ConflictException(ResponseMessages.REGISTRATION_EXISTS);
        }

        Bus bus = busRepository.save(Bus.create(schoolId, registration, request.model(), request.capacity()));

        publisher.publish(
                MessagingConstants.BUS_CREATED,
                new BusCreated(
                        bus.getId(),
                        bus.getSchoolId(),
                        bus.getRegistrationNumber(),
                        bus.getModel(),
                        bus.getCapacity(),
                        Instant.now()));

        return respondAndAnnounce(schoolId, bus);
    }

    @Transactional(readOnly = true)
    public PagedResult<BusResponse> list(
            UUID schoolId, String search, boolean includeInactive, int page, int pageSize) {
        int safePage = Math.max(page, 1);
        int safeSize = Math.min(Math.max(pageSize, 1), MAX_PAGE_SIZE);

        Pageable pageable = PageRequest.of(
                safePage - 1, safeSize, Sort.by("registrationNumber").ascending());

        // Reads as one sentence: buses of this school, optionally only the active
        // ones, optionally narrowed by a search term.
        Page<Bus> result = busRepository.findAll(
                BusSpecifications.forSchool(schoolId)
                        .and(BusSpecifications.activeOnly(includeInactive))
                        .and(BusSpecifications.matching(search)),
                pageable);

        // One document query for the whole page rather than one per bus.
        Map<UUID, Boolean> validity = busDocumentService.validityFor(
                schoolId, result.getContent().stream().map(Bus::getId).toList());

        List<BusResponse> items = result.getContent().stream()
                .map(bus -> busMapper.toResponse(bus, validity.getOrDefault(bus.getId(), false)))
                .toList();

        return new PagedResult<>(items, result.getTotalElements(), safePage, safeSize);
    }

    @Transactional(readOnly = true)
    public BusResponse getById(UUID schoolId, UUID id) {
        Bus bus = findOwned(schoolId, id);

        return busMapper.toResponse(bus, documentsValid(schoolId, bus));
    }

    @Transactional
    public BusResponse update(UUID schoolId, UUID id, UpdateBusRequest request) {
        Bus bus = findOwned(schoolId, id);

        String registration = Bus.normalizeRegistrationNumber(request.registrationNumber());
        if (busRepository.existsBySchoolIdAndRegistrationNumberAndIdNot(schoolId, registration, id)) {
            throw new AppException.ConflictException(ResponseMessages.REGISTRATION_EXISTS);
        }

        bus.update(registration, request.model(), request.capacity());

        // The registration number is on the event, so a rename has to be announced
        // or other services keep showing the old plate.
        return respondAndAnnounce(schoolId, bus);
    }

    @Transactional
    public BusResponse assignDriver(UUID schoolId, UUID id, AssignDriverRequest request) {
        requireApprovedSchool(schoolId);
        requireActiveSubscription(schoolId);
        Bus bus = findOwned(schoolId, id);

        bus.assignDriver(request.driverId());

        publisher.publish(
                MessagingConstants.BUS_DRIVER_ASSIGNED,
                new BusDriverAssigned(bus.getId(), schoolId, request.driverId(), Instant.now()));

        return busMapper.toResponse(bus, documentsValid(schoolId, bus));
    }

    @Transactional
    public void deactivate(UUID schoolId, UUID id) {
        Bus bus = findOwned(schoolId, id);

        if (bus.isActive()) {
            bus.deactivate();
            respondAndAnnounce(schoolId, bus);
        }
    }

    /**
     * Builds the response and tells everyone else, from one computation of the
     * document state — so the answer a caller gets and the answer other services
     * get can never disagree.
     */
    private BusResponse respondAndAnnounce(UUID schoolId, Bus bus) {
        boolean documentsValid = documentsValid(schoolId, bus);

        publisher.publish(
                MessagingConstants.BUS_STATUS_CHANGED,
                new BusStatusChanged(
                        bus.getId(),
                        schoolId,
                        bus.getRegistrationNumber(),
                        bus.isActive(),
                        documentsValid,
                        Instant.now()));

        return busMapper.toResponse(bus, documentsValid);
    }

    private boolean documentsValid(UUID schoolId, Bus bus) {
        return busDocumentService.validityFor(schoolId, List.of(bus.getId())).getOrDefault(bus.getId(), false);
    }

    private Bus findOwned(UUID schoolId, UUID id) {
        return busRepository
                .findByIdAndSchoolId(id, schoolId)
                .orElseThrow(() -> new AppException.NotFoundException(ResponseMessages.BUS_NOT_FOUND));
    }

    private void requireApprovedSchool(UUID schoolId) {
        if (!schoolStatusRepository.existsBySchoolIdAndStatus(schoolId, SchoolStatuses.APPROVED)) {
            throw new AppException.ForbiddenException(ResponseMessages.SCHOOL_NOT_APPROVED);
        }
    }

    /**
     * Refuses fleet changes once the subscription has lapsed.
     *
     * <p>Derived from the end date rather than trusting the status string. The
     * status arrived on an event and is a snapshot — a subscription that expired
     * this morning still reads "Active" here until the expiry job runs and its
     * event lands. A date cannot go stale.
     *
     * <p>Grace counts as live, matching the School service: the service keeps
     * working for seven days past the end date, and locking a school out earlier
     * than their own subscription page tells them would be a contradiction.
     *
     * <p>A school with no subscription record at all is permitted, for the same
     * reason requireBusCapacity permits a null limit — schools that predate
     * billing keep working, and introducing subscriptions must not lock anyone
     * out overnight.
     */
    private void requireActiveSubscription(UUID schoolId) {
        SchoolStatus status = schoolStatusRepository.findById(schoolId).orElse(null);

        if (status == null || status.getSubscriptionEndsOn() == null) {
            return;
        }

        if ("Cancelled".equalsIgnoreCase(status.getSubscriptionStatus())) {
            throw new AppException.ForbiddenException(ResponseMessages.SUBSCRIPTION_REQUIRED);
        }

        if (LocalDate.now(zone).isAfter(status.getSubscriptionEndsOn().plusDays(GRACE_DAYS))) {
            throw new AppException.ForbiddenException(ResponseMessages.SUBSCRIPTION_REQUIRED);
        }
    }

    /**
     * Refuses a new bus once the subscription's limit is reached.
     *
     * <p>This service never asks the School service anything. The limit arrived
     * on an event and is sitting in a local table, so the check costs one row
     * read and works whether or not School service is up.
     *
     * <p>A null limit means no enforcement — either the plan is unlimited, or no
     * subscription event has arrived for this school. That is deliberate:
     * schools created before subscriptions existed keep working, and introducing
     * billing does not lock anyone out of a bus service overnight.
     */
    private void requireBusCapacity(UUID schoolId) {
        Integer limit = schoolStatusRepository
                .findById(schoolId)
                .map(SchoolStatus::getBusLimit)
                .orElse(null);

        if (limit == null) {
            return;
        }

        // Only active buses count. A deactivated bus is not occupying a seat.
        long active =
                busRepository.count(BusSpecifications.forSchool(schoolId).and(BusSpecifications.activeOnly(false)));

        if (active >= limit) {
            throw new AppException.ForbiddenException(ResponseMessages.BUS_LIMIT_REACHED);
        }
    }
}
