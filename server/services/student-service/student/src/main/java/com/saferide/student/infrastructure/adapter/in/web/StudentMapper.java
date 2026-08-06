package com.saferide.student.infrastructure.adapter.in.web;

import com.saferide.student.domain.Student;
import com.saferide.student.infrastructure.adapter.in.web.dto.StudentResponse;
import org.mapstruct.Mapper;

@Mapper(componentModel = "spring")
public interface StudentMapper {
    StudentResponse toResponse(Student student);
}
