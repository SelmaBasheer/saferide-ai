package com.saferide.student.infrastructure.adapter.in.web.dto;

import java.util.List;

public record PagedResult<T>(List<T> items, long totalCount, int page, int pageSize) {}
