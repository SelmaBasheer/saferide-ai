package com.saferide.bus.controller;

import static com.saferide.bus.security.JwtClaims.schoolId;
import static com.saferide.bus.security.JwtClaims.userId;

import com.saferide.bus.constants.ResponseMessages;
import com.saferide.bus.dto.ApiResponse;
import com.saferide.bus.dto.BusDocumentResponse;
import com.saferide.bus.dto.DocumentLinkResponse;
import com.saferide.bus.entity.BusDocumentType;
import com.saferide.bus.service.BusDocumentService;
import java.time.LocalDate;
import java.util.List;
import java.util.UUID;
import org.springframework.format.annotation.DateTimeFormat;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RequestPart;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.multipart.MultipartFile;

@RestController
@RequestMapping("/api/buses/{busId}/documents")
public class BusDocumentController {

    private final BusDocumentService documentService;

    public BusDocumentController(BusDocumentService documentService) {
        this.documentService = documentService;
    }

    @PostMapping(consumes = MediaType.MULTIPART_FORM_DATA_VALUE)
    public ResponseEntity<ApiResponse<BusDocumentResponse>> upload(
            @AuthenticationPrincipal Jwt jwt,
            @PathVariable UUID busId,
            @RequestParam BusDocumentType type,
            @RequestParam @DateTimeFormat(iso = DateTimeFormat.ISO.DATE) LocalDate expiresOn,
            @RequestPart MultipartFile file) {
        BusDocumentResponse uploaded = documentService.upload(schoolId(jwt), busId, userId(jwt), type, expiresOn, file);

        return ResponseEntity.status(HttpStatus.CREATED)
                .body(ApiResponse.ok(uploaded, ResponseMessages.DOCUMENT_UPLOADED));
    }

    @GetMapping
    public ApiResponse<List<BusDocumentResponse>> list(@AuthenticationPrincipal Jwt jwt, @PathVariable UUID busId) {
        return ApiResponse.ok(documentService.list(schoolId(jwt), busId));
    }

    /**
     * Returns a link rather than the file. The browser then fetches it straight
     * from storage, so a 4 MB scan never passes through this service — and the
     * link stops working in five minutes.
     */
    @GetMapping("/{documentId}/link")
    public ApiResponse<DocumentLinkResponse> link(
            @AuthenticationPrincipal Jwt jwt, @PathVariable UUID busId, @PathVariable UUID documentId) {
        return ApiResponse.ok(documentService.downloadLink(schoolId(jwt), documentId));
    }
}
