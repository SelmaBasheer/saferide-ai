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
import com.saferide.bus.messaging.RabbitEventPublisher;
import com.saferide.bus.projection.SchoolStatusRepository;
import com.saferide.bus.projection.SchoolStatuses;
import com.saferide.bus.repository.BusRepository;
import java.time.Instant;
import java.util.List;
import java.util.UUID;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;
import org.springframework.data.domain.Sort;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class BusService {

    private static final int MAX_PAGE_SIZE = 50;

    private final BusRepository busRepository;
    private final SchoolStatusRepository schoolStatusRepository;
    private final BusMapper busMapper;
    private final RabbitEventPublisher publisher;

    public BusService(
            BusRepository busRepository,
            SchoolStatusRepository schoolStatusRepository,
            BusMapper busMapper,
            RabbitEventPublisher publisher) {
        this.busRepository = busRepository;
        this.schoolStatusRepository = schoolStatusRepository;
        this.busMapper = busMapper;
        this.publisher = publisher;
    }

    @Transactional
    public BusResponse create(UUID schoolId, CreateBusRequest request) {
        requireApprovedSchool(schoolId);

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

        return busMapper.toResponse(bus);
    }

    @Transactional(readOnly = true)
    public PagedResult<BusResponse> list(
            UUID schoolId, String search, boolean includeInactive, int page, int pageSize) {
        int safePage = Math.max(page, 1);
        int safeSize = Math.min(Math.max(pageSize, 1), MAX_PAGE_SIZE);

        String term = (search == null || search.isBlank()) ? null : search.trim();
        Pageable pageable = PageRequest.of(
                safePage - 1, safeSize, Sort.by("registrationNumber").ascending());

        Page<Bus> result = busRepository.search(schoolId, term, includeInactive, pageable);
        List<BusResponse> items =
                result.getContent().stream().map(busMapper::toResponse).toList();

        return new PagedResult<>(items, result.getTotalElements(), safePage, safeSize);
    }

    @Transactional(readOnly = true)
    public BusResponse getById(UUID schoolId, UUID id) {
        return busMapper.toResponse(findOwned(schoolId, id));
    }

    @Transactional
    public BusResponse update(UUID schoolId, UUID id, UpdateBusRequest request) {
        Bus bus = findOwned(schoolId, id);

        String registration = Bus.normalizeRegistrationNumber(request.registrationNumber());
        if (busRepository.existsBySchoolIdAndRegistrationNumberAndIdNot(schoolId, registration, id)) {
            throw new AppException.ConflictException(ResponseMessages.REGISTRATION_EXISTS);
        }

        bus.update(registration, request.model(), request.capacity());
        return busMapper.toResponse(bus);
    }

    @Transactional
    public BusResponse assignDriver(UUID schoolId, UUID id, AssignDriverRequest request) {
        requireApprovedSchool(schoolId);
        Bus bus = findOwned(schoolId, id);

        bus.assignDriver(request.driverId());

        publisher.publish(
                MessagingConstants.BUS_DRIVER_ASSIGNED,
                new BusDriverAssigned(bus.getId(), schoolId, request.driverId(), Instant.now()));

        return busMapper.toResponse(bus);
    }

    @Transactional
    public void deactivate(UUID schoolId, UUID id) {
        Bus bus = findOwned(schoolId, id);
        if (bus.isActive()) {
            bus.deactivate();
        }
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
}
