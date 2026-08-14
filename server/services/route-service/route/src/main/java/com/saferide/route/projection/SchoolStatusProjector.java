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
public class SchoolStatusProjector {

    private static final Logger log = LoggerFactory.getLogger(SchoolStatusProjector.class);

    private final MongoTemplate mongo;

    public SchoolStatusProjector(MongoTemplate mongo) {
        this.mongo = mongo;
    }

    /** Atomic conditional update: applies the event only if it is newer than the stored row. */
    public void upsert(UUID schoolId, String status, Instant occurredAtUtc) {
        Query newerThanStored = new Query(Criteria.where("_id")
                .is(schoolId)
                .orOperator(
                        Criteria.where("eventAtUtc").is(null),
                        Criteria.where("eventAtUtc").lt(occurredAtUtc)));

        Update update = new Update()
                .set("status", status)
                .set("eventAtUtc", occurredAtUtc)
                .set("updatedAt", Instant.now());

        long matched =
                mongo.updateFirst(newerThanStored, update, SchoolStatus.class).getMatchedCount();

        if (matched > 0) {
            log.info("School {} projected as {}", schoolId, status);
            return;
        }

        try {
            mongo.insert(new SchoolStatus(schoolId, status, occurredAtUtc));
            log.info("School {} projected as {} (first event)", schoolId, status);
        } catch (DuplicateKeyException e) {
            log.debug("Ignoring stale school event for {}: event={}", schoolId, occurredAtUtc);
        }
    }
}
