package com.saferide.bus.mapper;

import com.saferide.bus.dto.BusResponse;
import com.saferide.bus.entity.Bus;
import org.mapstruct.Mapper;

@Mapper(componentModel = "spring")
public interface BusMapper {

    BusResponse toResponse(Bus bus);
}
