cmake_minimum_required(VERSION 3.16)

project(serial_hid_core VERSION 0.1.0 LANGUAGES C)

include(GNUInstallDirs)
include(CheckLanguage)

option(SERIAL_HID_CORE_BUILD_CPP_WRAPPERS "Build C++ wrapper target when C++ is available" ON)
option(SERIAL_HID_CORE_BUILD_SHARED "Build shared libraries instead of static" OFF)

if(SERIAL_HID_CORE_BUILD_SHARED)
    set(_shd_lib_type SHARED)
else()
    set(_shd_lib_type STATIC)
endif()

add_library(serial_hid_core ${_shd_lib_type})
add_library(serial_hid_core::c ALIAS serial_hid_core)

target_sources(serial_hid_core
    PRIVATE
        src/serial_hid_core.c
)

target_include_directories(serial_hid_core
    PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
        $<INSTALL_INTERFACE:${CMAKE_INSTALL_INCLUDEDIR}>
)

target_compile_features(serial_hid_core PUBLIC c_std_99)
set_target_properties(serial_hid_core PROPERTIES
    OUTPUT_NAME serial_hid_core
)

check_language(CXX)
if(SERIAL_HID_CORE_BUILD_CPP_WRAPPERS AND CMAKE_CXX_COMPILER)
    enable_language(CXX)

    add_library(serial_hid_core_cpp ${_shd_lib_type})
    add_library(serial_hid_core::cpp ALIAS serial_hid_core_cpp)

    target_sources(serial_hid_core_cpp
        PRIVATE
            src/serial_hid_cpp.cpp
    )

    target_include_directories(serial_hid_core_cpp
        PUBLIC
            $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
            $<INSTALL_INTERFACE:${CMAKE_INSTALL_INCLUDEDIR}>
    )

    target_compile_features(serial_hid_core_cpp PUBLIC cxx_std_11)
    set_target_properties(serial_hid_core_cpp PROPERTIES
        OUTPUT_NAME serial_hid_core_cpp
    )

    target_link_libraries(serial_hid_core_cpp PUBLIC serial_hid_core)
elseif(SERIAL_HID_CORE_BUILD_CPP_WRAPPERS)
    message(STATUS "C++ compiler not available, skipping serial_hid_core_cpp target")
endif()

install(TARGETS serial_hid_core
    ARCHIVE DESTINATION ${CMAKE_INSTALL_LIBDIR}
    LIBRARY DESTINATION ${CMAKE_INSTALL_LIBDIR}
    RUNTIME DESTINATION ${CMAKE_INSTALL_BINDIR}
)

if(TARGET serial_hid_core_cpp)
    install(TARGETS serial_hid_core_cpp
        ARCHIVE DESTINATION ${CMAKE_INSTALL_LIBDIR}
        LIBRARY DESTINATION ${CMAKE_INSTALL_LIBDIR}
        RUNTIME DESTINATION ${CMAKE_INSTALL_BINDIR}
    )
endif()

install(FILES
    include/serial_hid_types.h
    include/serial_hid_interfaces.h
    include/serial_hid_core.h
    include/serial_hid_cpp.hpp
    DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}
)
