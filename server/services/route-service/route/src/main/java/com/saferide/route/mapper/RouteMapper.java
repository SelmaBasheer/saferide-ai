package com.saferide.route.mapper;

import com.saferide.route.document.Route;
import com.saferide.route.document.Stop;
import com.saferide.route.dto.GeoPointDto;
import com.saferide.route.dto.RouteResponse;
import com.saferide.route.dto.StopDto;
import java.util.List;
import org.mapstruct.Mapper;
import org.springframework.data.mongodb.core.geo.GeoJsonLineString;

@Mapper(componentModel = "spring")
public interface RouteMapper {

    RouteResponse toResponse(Route route);

    default StopDto toStopDto(Stop stop) {
        if (stop == null) {
            return null;
        }
        return new StopDto(
                stop.getStopId(),
                stop.getSequence(),
                stop.getName(),
                stop.getLatitude(),
                stop.getLongitude(),
                stop.getPickupTime());
    }

    default List<GeoPointDto> toPath(GeoJsonLineString path) {
        if (path == null) {
            return null;
        }
        return path.getCoordinates().stream()
                .map(p -> new GeoPointDto(p.getY(), p.getX())) // y = lat, x = lng
                .toList();
    }
}
