package com.saferide.bus.mapper;

import com.saferide.bus.dto.BusResponse;
import com.saferide.bus.entity.Bus;
import org.mapstruct.Mapper;
import org.mapstruct.Mapping;

@Mapper(componentModel = "spring")
public interface BusMapper {

    @Mapping(target = "documentsValid", source = "documentsValid")
    BusResponse toResponse(Bus bus, boolean documentsValid);
}
