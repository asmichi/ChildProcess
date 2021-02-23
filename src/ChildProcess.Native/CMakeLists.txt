# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#
# CMakeLists.txt for building ChildProcess.Native (not including .NET projects.)
#

cmake_minimum_required(VERSION 3.10)

project("AsmichiChildProcessNative" CXX)

set(objlibName "objlib")
set(libName "AsmichiChildProcess")
set(helperName "AsmichiChildProcessHelper")
set(testChildName "TestChildNative")
set(versionScript "${CMAKE_CURRENT_SOURCE_DIR}/AsmichiChildProcess.version")
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
set(CMAKE_BUILD_WITH_INSTALL_RPATH true)
set(CMAKE_INSTALL_RPATH "\$ORIGIN")

find_package(Threads REQUIRED)

set(objlibSources
    AncillaryDataSocket.cpp
    Base.cpp
    ChildProcessState.cpp
    Globals.cpp
    Exports.cpp
    HelperMain.cpp
    MiscHelpers.cpp
    Request.cpp
    Service.cpp
    SignalHandler.cpp
    Subchannel.cpp
    SubchannelCollection.cpp
    SocketHelpers.cpp
    WriteBuffer.cpp
)

set(helperSources
    HelperExecutable.cpp
)

if(WIN32)

    # Assume MSVC
    add_compile_options(
        /W3
        $<$<OR:$<CONFIG:Release>,$<CONFIG:RelWithDebInfo>>:/O2>
        $<$<CONFIG:Debug>:/Od>
        $<$<CONFIG:Debug>:/DENABLE_TRACE_DEBUG>
        $<$<CONFIG:Debug>:/DENABLE_TRACE_INFO>
        $<$<CONFIG:Debug>:/DENABLE_TRACE_ERROR>
    )

    #
    # The helper executable for tests.
    #
    set(testChildSources
        tests/TestChildMain.win32.cpp
    )
    add_executable(${testChildName} ${testChildSources}>)
    target_include_directories(${testChildName} PRIVATE include)
    target_compile_features(${testChildName} PRIVATE cxx_std_17)
    target_link_libraries(${testChildName})

else(WIN32)

    add_compile_options(
        -Wextra
        -Wswitch
        $<$<OR:$<CONFIG:Release>,$<CONFIG:RelWithDebInfo>>:-O3>
        $<$<CONFIG:Debug>:-O1>
        $<$<CONFIG:Debug>:-fno-omit-frame-pointer>
        $<$<CONFIG:Debug>:-DENABLE_TRACE_DEBUG>
        $<$<CONFIG:Debug>:-DENABLE_TRACE_INFO>
        $<$<CONFIG:Debug>:-DENABLE_TRACE_ERROR>
    )

    #
    # An intermediate object library to allow tests to reference nonexported symbols by statically linking these objects.
    #
    add_library(${objlibName} OBJECT ${objlibSources})
    target_compile_features(${objlibName} PRIVATE cxx_std_17)
    target_include_directories(${objlibName} PUBLIC include)
    # These objects are primarily used for producing a shared library, hence POSITION_INDEPENDENT_CODE.
    set_property(TARGET ${objlibName} PROPERTY POSITION_INDEPENDENT_CODE 1)

    #
    # The shared library used by both the C# implementation and the helper.
    #
    add_library(${libName} SHARED $<TARGET_OBJECTS:${objlibName}>)
    target_compile_features(${libName} PRIVATE cxx_std_17)
    target_link_libraries(${libName}
        Threads::Threads
        ${CMAKE_DL_LIBS}
    )
    set_target_properties(${libName} PROPERTIES LINK_DEPENDS ${versionScript})
    set_target_properties(${libName} PROPERTIES LINK_FLAGS
        "-Wl,--version-script=${versionScript} -Wl,--no-undefined"
    )

    #
    # The helper process.
    #
    add_executable(${helperName} ${helperSources})
    target_compile_features(${helperName} PRIVATE cxx_std_17)
    target_link_libraries(${helperName}
        ${libName}
        Threads::Threads
        ${CMAKE_DL_LIBS}
    )

    #
    # The helper executable for tests.
    #
    set(testChildSources
        tests/TestChildMain.unix.cpp
    )
    add_executable(${testChildName} ${testChildSources} $<TARGET_OBJECTS:${objlibName}>)
    target_include_directories(${testChildName} PRIVATE include)
    target_compile_features(${testChildName} PRIVATE cxx_std_17)
    target_link_libraries(${testChildName}
        Threads::Threads
        ${CMAKE_DL_LIBS}
    )
endif(WIN32)
