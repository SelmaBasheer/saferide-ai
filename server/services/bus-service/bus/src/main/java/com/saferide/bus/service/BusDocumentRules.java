package com.saferide.bus.service;

import com.saferide.bus.entity.BusDocument;
import com.saferide.bus.entity.BusDocumentType;
import java.time.LocalDate;
import java.util.Collection;
import java.util.EnumMap;
import java.util.Map;

/**
 * A bus is road-legal when it holds all three certificates and none has expired.
 * Pure and takes today's date, so it can be tested on both sides of an expiry.
 */
public final class BusDocumentRules {

    private BusDocumentRules() {}

    public static boolean allValid(Collection<BusDocument> documents, LocalDate today) {
        Map<BusDocumentType, BusDocument> newest = newestByType(documents);

        for (BusDocumentType type : BusDocumentType.values()) {
            BusDocument document = newest.get(type);

            if (document == null || document.isExpired(today)) {
                return false;
            }
        }

        return true;
    }

    /** Documents are never replaced, only superseded, so the current one is the latest upload. */
    public static Map<BusDocumentType, BusDocument> newestByType(Collection<BusDocument> documents) {
        Map<BusDocumentType, BusDocument> newest = new EnumMap<>(BusDocumentType.class);

        for (BusDocument document : documents) {
            BusDocument held = newest.get(document.getType());

            if (held == null || document.getUploadedAt().isAfter(held.getUploadedAt())) {
                newest.put(document.getType(), document);
            }
        }

        return newest;
    }
}
