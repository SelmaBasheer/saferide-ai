package com.saferide.route.dto;

import java.util.UUID;

public record StopDto(UUID stopId, int sequence, String name, double latitude, double longitude, String pickupTime) {}
