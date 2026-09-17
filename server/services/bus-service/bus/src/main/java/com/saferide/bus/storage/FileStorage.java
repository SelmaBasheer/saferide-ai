package com.saferide.bus.storage;

import java.io.InputStream;
import java.net.URI;
import java.time.Duration;

public interface FileStorage {

    void upload(String key, InputStream content, long sizeBytes, String contentType);

    void delete(String key);

    // A time-limited URL the browser can fetch directly, so the file never passes through this service.
    URI downloadUrl(String key, String downloadFileName, Duration validFor);
}
