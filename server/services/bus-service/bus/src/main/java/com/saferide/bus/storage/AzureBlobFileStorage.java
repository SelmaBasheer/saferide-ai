package com.saferide.bus.storage;

import com.azure.core.util.BinaryData;
import com.azure.storage.blob.BlobClient;
import com.azure.storage.blob.BlobContainerClient;
import com.azure.storage.blob.BlobContainerClientBuilder;
import com.azure.storage.blob.models.BlobHttpHeaders;
import com.azure.storage.blob.sas.BlobSasPermission;
import com.azure.storage.blob.sas.BlobServiceSasSignatureValues;
import java.io.InputStream;
import java.net.URI;
import java.time.Duration;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

@Component
public class AzureBlobFileStorage implements FileStorage {

    private final BlobContainerClient container;

    /** Where this service reaches storage. */
    private final String privatePrefix;

    /**
     * Where a browser reaches the same storage. Usually identical — but inside a
     * container they differ, because the service and the person clicking the link
     * are on different networks. A pre-signed URL is the one address this service
     * hands out for somebody else to resolve, so it has to be signed with the name
     * that works over there rather than the one that works here.
     */
    private final String publicPrefix;

    public AzureBlobFileStorage(
            @Value("${saferide.storage.connection-string}") String connectionString,
            @Value("${saferide.storage.container}") String containerName,
            @Value("${saferide.storage.public-endpoint:}") String publicEndpoint) {
        this.container = new BlobContainerClientBuilder()
                .connectionString(connectionString)
                .containerName(containerName)
                .buildClient();

        container.createIfNotExists();

        this.privatePrefix = container.getBlobContainerUrl();
        this.publicPrefix =
                publicEndpoint.isBlank() ? privatePrefix : publicEndpoint.replaceAll("/+$", "") + "/" + containerName;
    }

    @Override
    public void upload(String key, InputStream content, long sizeBytes, String contentType) {
        BlobClient blob = container.getBlobClient(key);

        // true overwrites: re-uploading the same key replaces it rather than
        // failing. Keys carry a UUID, so this only matters on a retry.
        blob.upload(BinaryData.fromStream(content, sizeBytes), true);
        blob.setHttpHeaders(new BlobHttpHeaders().setContentType(contentType));
    }

    @Override
    public void delete(String key) {
        container.getBlobClient(key).deleteIfExists();
    }

    @Override
    public URI downloadUrl(String key, String downloadFileName, Duration validFor) {
        BlobClient blob = container.getBlobClient(key);

        // Content-Disposition is set on the SAS, not on the blob, so the browser
        // saves the file under the name the user originally uploaded even though
        // the blob itself is named by a UUID.
        BlobServiceSasSignatureValues values = new BlobServiceSasSignatureValues(
                        OffsetDateTime.now(ZoneOffset.UTC).plus(validFor),
                        new BlobSasPermission().setReadPermission(true))
                .setContentDisposition("attachment; filename=\"" + downloadFileName + "\"");

        // The signature covers the account, container and blob path — never the
        // hostname — so swapping the host afterwards leaves it valid.
        String signed = blob.getBlobUrl() + "?" + blob.generateSas(values);

        return URI.create(signed.replace(privatePrefix, publicPrefix));
    }
}
