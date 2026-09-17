package com.saferide.bus.service;

import com.saferide.bus.constants.MessagingConstants;
import com.saferide.bus.constants.ResponseMessages;
import com.saferide.bus.dto.BusDocumentResponse;
import com.saferide.bus.dto.DocumentLinkResponse;
import com.saferide.bus.entity.Bus;
import com.saferide.bus.entity.BusDocument;
import com.saferide.bus.entity.BusDocumentType;
import com.saferide.bus.exception.AppException;
import com.saferide.bus.messaging.BusStatusChanged;
import com.saferide.bus.messaging.RabbitEventPublisher;
import com.saferide.bus.repository.BusDocumentRepository;
import com.saferide.bus.repository.BusRepository;
import com.saferide.bus.storage.FileStorage;
import java.io.IOException;
import java.io.InputStream;
import java.net.URI;
import java.time.Duration;
import java.time.Instant;
import java.time.LocalDate;
import java.util.Collection;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.UUID;
import java.util.stream.Collectors;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.multipart.MultipartFile;

@Service
public class BusDocumentService {

    private static final Set<String> ALLOWED_CONTENT_TYPES = Set.of("application/pdf", "image/jpeg", "image/png");

    /** Spring rejects anything larger before it reaches us; this is the second line. */
    private static final long MAX_SIZE_BYTES = 5L * 1024 * 1024;

    /** Matches the column width on BusDocument.originalFileName. */
    private static final int MAX_FILE_NAME_LENGTH = 260;

    /** Long enough to click, short enough that a copied link is useless tomorrow. */
    private static final Duration LINK_VALIDITY = Duration.ofMinutes(5);

    private static final byte[] PDF_MAGIC = {0x25, 0x50, 0x44, 0x46};
    private static final byte[] JPEG_MAGIC = {(byte) 0xFF, (byte) 0xD8, (byte) 0xFF};
    private static final byte[] PNG_MAGIC = {(byte) 0x89, 0x50, 0x4E, 0x47};

    private final BusRepository busRepository;
    private final BusDocumentRepository documentRepository;
    private final FileStorage storage;
    private final RabbitEventPublisher publisher;

    public BusDocumentService(
            BusRepository busRepository,
            BusDocumentRepository documentRepository,
            FileStorage storage,
            RabbitEventPublisher publisher) {
        this.busRepository = busRepository;
        this.documentRepository = documentRepository;
        this.storage = storage;
        this.publisher = publisher;
    }

    @Transactional
    public BusDocumentResponse upload(
            UUID schoolId, UUID busId, UUID userId, BusDocumentType type, LocalDate expiresOn, MultipartFile file) {
        Bus bus = requireOwnedBus(schoolId, busId);

        // The page only offers uploading for an active bus, but the endpoint is
        // reachable directly. A rule the UI enforces is not a rule.
        if (!bus.isActive()) {
            throw new AppException.ConflictException(ResponseMessages.BUS_NOT_ACTIVE);
        }

        validate(file, expiresOn, LocalDate.now());

        // The declared content type comes from the browser and can say anything.
        // The first few bytes cannot.
        String detectedContentType = detectContentType(file);
        String fileName = safeFileName(file.getOriginalFilename());

        String blobName = "buses/%s/%s/%s/%s".formatted(schoolId, busId, type, UUID.randomUUID());

        try (InputStream content = file.getInputStream()) {
            storage.upload(blobName, content, file.getSize(), detectedContentType);
        } catch (IOException e) {
            throw new AppException.ValidationException(ResponseMessages.DOCUMENT_UNREADABLE);
        }

        // Blob first, row second. An orphaned blob wastes a few kilobytes and
        // nobody notices; a row pointing at a file that was never written is a
        // download that fails forever.
        BusDocument document = BusDocument.upload(
                bus.getId(),
                schoolId,
                type,
                blobName,
                fileName,
                detectedContentType,
                file.getSize(),
                expiresOn,
                userId);

        documentRepository.save(document);

        // Recomputed after the save, so it accounts for the certificate just
        // uploaded. Anything gating on a bus's paperwork hears about it here.
        boolean documentsValid = validityFor(schoolId, List.of(busId)).getOrDefault(busId, false);

        publisher.publish(
                MessagingConstants.BUS_STATUS_CHANGED,
                new BusStatusChanged(
                        bus.getId(),
                        schoolId,
                        bus.getRegistrationNumber(),
                        bus.isActive(),
                        documentsValid,
                        Instant.now()));

        return toResponse(document, LocalDate.now());
    }

    @Transactional(readOnly = true)
    public List<BusDocumentResponse> list(UUID schoolId, UUID busId) {
        requireOwnedBus(schoolId, busId);

        LocalDate today = LocalDate.now();

        return documentRepository.findBySchoolIdAndBusIdOrderByUploadedAtDesc(schoolId, busId).stream()
                .map(document -> toResponse(document, today))
                .toList();
    }

    /**
     * Whether each of these buses holds all three certificates, unexpired. One
     * query for the whole page — the alternative is one per bus, which is the
     * classic way a list endpoint quietly becomes slow.
     */
    @Transactional(readOnly = true)
    public Map<UUID, Boolean> validityFor(UUID schoolId, Collection<UUID> busIds) {
        if (busIds.isEmpty()) {
            return Map.of();
        }

        LocalDate today = LocalDate.now();

        Map<UUID, List<BusDocument>> byBus = documentRepository.findBySchoolIdAndBusIdIn(schoolId, busIds).stream()
                .collect(Collectors.groupingBy(BusDocument::getBusId));

        Map<UUID, Boolean> validity = new HashMap<>();

        for (UUID busId : busIds) {
            validity.put(busId, BusDocumentRules.allValid(byBus.getOrDefault(busId, List.of()), today));
        }

        return validity;
    }

    @Transactional(readOnly = true)
    public DocumentLinkResponse downloadLink(UUID schoolId, UUID documentId) {
        // Scoped by schoolId, so another school's document id is a 404 rather
        // than a 403 — a 403 would confirm the document exists.
        BusDocument document = documentRepository
                .findByIdAndSchoolId(documentId, schoolId)
                .orElseThrow(() -> new AppException.NotFoundException(ResponseMessages.DOCUMENT_NOT_FOUND));

        URI url = storage.downloadUrl(document.getBlobName(), document.getOriginalFileName(), LINK_VALIDITY);

        return new DocumentLinkResponse(url.toString(), LINK_VALIDITY.toSeconds());
    }

    private Bus requireOwnedBus(UUID schoolId, UUID busId) {
        return busRepository
                .findByIdAndSchoolId(busId, schoolId)
                .orElseThrow(() -> new AppException.NotFoundException(ResponseMessages.BUS_NOT_FOUND));
    }

    /** Today is passed in rather than read here, so the rule can be tested. */
    private static void validate(MultipartFile file, LocalDate expiresOn, LocalDate today) {
        if (file == null || file.isEmpty()) {
            throw new AppException.ValidationException(ResponseMessages.DOCUMENT_FILE_REQUIRED);
        }

        if (file.getSize() > MAX_SIZE_BYTES) {
            throw new AppException.ValidationException(ResponseMessages.DOCUMENT_TOO_LARGE);
        }

        if (expiresOn == null || !expiresOn.isAfter(today)) {
            throw new AppException.ValidationException(ResponseMessages.DOCUMENT_EXPIRY_IN_PAST);
        }
    }

    /**
     * Reads the first bytes and decides what the file actually is. A caller can
     * label anything application/pdf; it cannot make arbitrary bytes start with
     * %PDF. The detected type is what gets stored, so the blob's own headers
     * never repeat the caller's claim.
     */
    private static String detectContentType(MultipartFile file) {
        byte[] head = new byte[8];

        try (InputStream in = file.getInputStream()) {
            in.readNBytes(head, 0, head.length);
        } catch (IOException e) {
            throw new AppException.ValidationException(ResponseMessages.DOCUMENT_UNREADABLE);
        }

        String detected = null;

        if (startsWith(head, PDF_MAGIC)) {
            detected = "application/pdf";
        } else if (startsWith(head, JPEG_MAGIC)) {
            detected = "image/jpeg";
        } else if (startsWith(head, PNG_MAGIC)) {
            detected = "image/png";
        }

        if (detected == null || !ALLOWED_CONTENT_TYPES.contains(detected)) {
            throw new AppException.ValidationException(ResponseMessages.DOCUMENT_TYPE_NOT_ALLOWED);
        }

        return detected;
    }

    private static boolean startsWith(byte[] content, byte[] magic) {
        if (content.length < magic.length) {
            return false;
        }

        for (int i = 0; i < magic.length; i++) {
            if (content[i] != magic[i]) {
                return false;
            }
        }

        return true;
    }

    /**
     * Some browsers send a full path, and this name ends up inside a
     * Content-Disposition header on the download link — so strip directories and
     * quotes rather than trusting what arrived. Truncated to the column width
     * here rather than after the upload, so an over-long name fails before
     * anything is stored.
     */
    private static String safeFileName(String original) {
        if (original == null || original.isBlank()) {
            return "document";
        }

        int lastSeparator = Math.max(original.lastIndexOf('/'), original.lastIndexOf('\\'));
        String name = original.substring(lastSeparator + 1).replace("\"", "").trim();

        if (name.isBlank()) {
            return "document";
        }

        if (name.length() > MAX_FILE_NAME_LENGTH) {
            int dot = name.lastIndexOf('.');
            String extension = dot >= 0 ? name.substring(dot) : "";
            name = name.substring(0, MAX_FILE_NAME_LENGTH - extension.length()) + extension;
        }

        return name;
    }

    private static BusDocumentResponse toResponse(BusDocument document, LocalDate today) {
        return new BusDocumentResponse(
                document.getId(),
                document.getType().name(),
                document.getOriginalFileName(),
                document.getSizeBytes(),
                document.getExpiresOn(),
                document.isExpired(today),
                document.getUploadedAt());
    }
}
