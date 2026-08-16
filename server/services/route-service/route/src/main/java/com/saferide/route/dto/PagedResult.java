package com.saferide.route.dto;

import java.util.List;

public record PagedResult<T>(List<T> items, long totalCount, int page, int pageSize) {}
