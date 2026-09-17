package com.saferide.bus.constants;

public final class ResponseMessages {

    public static final String BUS_CREATED = "Bus created successfully.";
    public static final String BUS_UPDATED = "Bus updated successfully.";
    public static final String BUS_DEACTIVATED = "Bus deactivated successfully.";
    public static final String DRIVER_ASSIGNED = "Driver assigned successfully.";
    public static final String DOCUMENT_UPLOADED = "Document uploaded successfully.";

    public static final String BUS_NOT_FOUND = "Bus not found.";
    public static final String REGISTRATION_EXISTS = "A bus with this registration number already exists.";
    public static final String SCHOOL_NOT_APPROVED = "This school is not approved to manage buses.";
    public static final String MISSING_SCHOOL_CLAIM = "Token does not carry a school identifier.";
    public static final String MISSING_USER_CLAIM = "Token does not carry a user identifier.";

    public static final String DOCUMENT_NOT_FOUND = "Document not found.";
    public static final String DOCUMENT_FILE_REQUIRED = "A file is required.";
    public static final String DOCUMENT_TOO_LARGE = "The file must be 5 MB or smaller.";
    public static final String DOCUMENT_TYPE_NOT_ALLOWED = "Only PDF, JPEG and PNG files are accepted.";
    public static final String DOCUMENT_UNREADABLE = "The uploaded file could not be read.";
    public static final String DOCUMENT_EXPIRY_IN_PAST = "The expiry date must be in the future.";
    public static final String BUS_NOT_ACTIVE = "This bus is not active.";

    private ResponseMessages() {}
}
