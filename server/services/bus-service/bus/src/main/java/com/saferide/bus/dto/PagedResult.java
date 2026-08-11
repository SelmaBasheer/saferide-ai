package com.saferide.bus.dto;

import java.util.List;

public record PagedResult<T>(List<T> items, long totalCount, int page, int pageSize) {}
