package com.saferide.route.projection;

import java.time.Instant;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DuplicateKeyException;
import org.springframework.data.mongodb.core.MongoTemplate;
import org.springframework.data.mongodb.core.query.Criteria;
import org.springframework.data.mongodb.core.query.Query;
import org.springframework.data.mongodb.core.query.Update;
import org.springframework.stereotype.Component;

@Component
public class BusStatusProjector {

    private static final Logger log = LoggerFactory.getLogger(BusStatusProjector.class);

    private final MongoTemplate mongo;

    public BusStatusProjector(MongoTemplate mongo) {
        this.mongo = mongo;
    }

    /** Atomic conditional update: applies the event only if it is newer than the stored row. */
    public void upsert(
            UUID busId,
            UUID schoolId,
            String registrationNumber,
            boolean active,
            boolean documentsValid,
            Instant occurredAtUtc) {
        Query newerThanStored = new Query(Criteria.where("_id")
                .is(busId)
                .orOperator(
                        Criteria.where("eventAtUtc").is(null),
                        Criteria.where("eventAtUtc").lt(occurredAtUtc)));

        Update update = new Update()
                .set("schoolId", schoolId)
                .set("registrationNumber", registrationNumber)
                .set("active", active)
                .set("documentsValid", documentsValid)
                .set("eventAtUtc", occurredAtUtc)
                .set("updatedAt", Instant.now());

        long matched =
                mongo.updateFirst(newerThanStored, update, BusStatus.class).getMatchedCount();

        if (matched > 0) {
            log.info("Bus {} projected: active={}, documentsValid={}", busId, active, documentsValid);
            return;
        }

        try {
            mongo.insert(new BusStatus(busId, schoolId, registrationNumber, active, documentsValid, occurredAtUtc));
            log.info("Bus {} projected (first event)", busId);
        } catch (DuplicateKeyException e) {
            long retried =
                    mongo.updateFirst(newerThanStored, update, BusStatus.class).getMatchedCount();
            if (retried > 0) {
                log.info("Bus {} projected (after insert race)", busId);
            } else {
                log.debug("Ignoring stale bus event for {}: event={}", busId, occurredAtUtc);
            }
        }
    }
}
