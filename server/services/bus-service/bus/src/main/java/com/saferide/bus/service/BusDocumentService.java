package com.saferide.bus.service;

import com.saferide.bus.constants.ResponseMessages;
import com.saferide.bus.dto.BusDocumentResponse;
import com.saferide.bus.dto.DocumentLinkResponse;
import com.saferide.bus.entity.Bus;
import com.saferide.bus.entity.BusDocument;
import com.saferide.bus.entity.BusDocumentType;
import com.saferide.bus.exception.AppException;
import com.saferide.bus.repository.BusDocumentRepository;
import com.saferide.bus.repository.BusRepository;
import com.saferide.bus.storage.FileStorage;
import java.io.IOException;
import java.io.InputStream;
import java.net.URI;
import java.time.Duration;
import java.time.LocalDate;
import java.util.List;
import java.util.Set;
import java.util.UUID;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.multipart.MultipartFile;

@Service
public class BusDocumentService {

    private static final Set<String> ALLOWED_CONTENT_TYPES = Set.of("application/pdf", "image/jpeg", "image/png");
    private static final long MAX_SIZE_BYTES = 5L * 1024 * 1024;
    private static final Duration LINK_VALIDITY = Duration.ofMinutes(5);

    private final BusRepository busRepository;
    private final BusDocumentRepository documentRepository;
    private final FileStorage storage;

    public BusDocumentService(
            BusRepository busRepository, BusDocumentRepository documentRepository, FileStorage storage) {
        this.busRepository = busRepository;
        this.documentRepository = documentRepository;
        this.storage = storage;
    }

    @Transactional
    public BusDocumentResponse upload(
            UUID schoolId, UUID busId, UUID userId, BusDocumentType type, LocalDate expiresOn, MultipartFile file) {
        Bus bus = requireOwnedBus(schoolId, busId);
        validate(file, expiresOn, LocalDate.now());

        String blobName = "buses/%s/%s/%s/%s".formatted(schoolId, busId, type, UUID.randomUUID());

        try (InputStream content = file.getInputStream()) {
            storage.upload(blobName, content, file.getSize(), file.getContentType());
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
                safeFileName(file.getOriginalFilename()),
                file.getContentType(),
                file.getSize(),
                expiresOn,
                userId);

        documentRepository.save(document);

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

    private static void validate(MultipartFile file, LocalDate expiresOn, LocalDate today) {
        if (file == null || file.isEmpty()) {
            throw new AppException.ValidationException(ResponseMessages.DOCUMENT_FILE_REQUIRED);
        }

        if (file.getSize() > MAX_SIZE_BYTES) {
            throw new AppException.ValidationException(ResponseMessages.DOCUMENT_TOO_LARGE);
        }

        if (file.getContentType() == null || !ALLOWED_CONTENT_TYPES.contains(file.getContentType())) {
            throw new AppException.ValidationException(ResponseMessages.DOCUMENT_TYPE_NOT_ALLOWED);
        }

        if (expiresOn == null || !expiresOn.isAfter(today)) {
            throw new AppException.ValidationException(ResponseMessages.DOCUMENT_EXPIRY_IN_PAST);
        }
    }

    /**
     * Some browsers send a full path, and this name ends up inside a
     * Content-Disposition header on the download link — so strip directories and
     * quotes rather than trusting what arrived.
     */
    private static String safeFileName(String original) {
        if (original == null || original.isBlank()) {
            return "document";
        }

        int lastSeparator = Math.max(original.lastIndexOf('/'), original.lastIndexOf('\\'));
        String name = original.substring(lastSeparator + 1).replace("\"", "").trim();

        return name.isBlank() ? "document" : name;
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
