cmake_minimum_required(VERSION 3.20)

project(QuickDungeonGeneration LANGUAGES CXX)

# --- 1. NEW: Native Shared Library for Unity Plugin ---
add_library(NativeLevelGen SHARED
    src/DungeonInterop.cpp
    src/DungeonGenerator.cpp
    src/RandomWalkRoomGenerator.cpp
    src/PoissonDiskSampler.cpp
    src/MSTSolver.cpp
    src/DisjointSet.cpp
    src/AStarPathFinder.cpp
    src/OrthogonalRoutingPathfinder.cpp
    src/WallThicknessEnforcer.cpp
)

target_include_directories(NativeLevelGen
    PRIVATE
        ${PROJECT_SOURCE_DIR}/include
)

target_compile_features(NativeLevelGen
    PRIVATE
        cxx_std_20
)

# Automatically drop compiled shared library straight into your Unity package folder
set_target_properties(NativeLevelGen PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${PROJECT_SOURCE_DIR}/Com.JohnReynolds.QuickDungeonGeneration/Runtime/Plugins/x86_64"
    LIBRARY_OUTPUT_DIRECTORY "${PROJECT_SOURCE_DIR}/Com.JohnReynolds.QuickDungeonGeneration/Runtime/Plugins/x86_64"
    ARCHIVE_OUTPUT_DIRECTORY "${PROJECT_SOURCE_DIR}/Com.JohnReynolds.QuickDungeonGeneration/Runtime/Plugins/x86_64"
)

# --- FIX: Strip out MSBuild/Visual Studio Debug/Release subfolders ---
foreach(OUTPUTCONFIG ${CMAKE_CONFIGURATION_TYPES})
    string(TOUPPER ${OUTPUTCONFIG} OUTPUTCONFIG)
    set_target_properties(NativeLevelGen PROPERTIES
        RUNTIME_OUTPUT_DIRECTORY_${OUTPUTCONFIG} "${PROJECT_SOURCE_DIR}/Com.JohnReynolds.QuickDungeonGeneration/Runtime/Plugins/x86_64"
        LIBRARY_OUTPUT_DIRECTORY_${OUTPUTCONFIG} "${PROJECT_SOURCE_DIR}/Com.JohnReynolds.QuickDungeonGeneration/Runtime/Plugins/x86_64"
        ARCHIVE_OUTPUT_DIRECTORY_${OUTPUTCONFIG} "${PROJECT_SOURCE_DIR}/Com.JohnReynolds.QuickDungeonGeneration/Runtime/Plugins/x86_64"
    )
endforeach()

# --- 2. EXISTING EXECUTABLES & TESTS ---

add_executable(QuickDungeonGeneration
    tests/UserInputSeedTest.cpp
    src/AStarPathFinder.cpp
    src/DungeonGenerator.cpp
    src/RandomWalkRoomGenerator.cpp
    src/PoissonDiskSampler.cpp
    src/MSTSolver.cpp
    src/DisjointSet.cpp
    src/OrthogonalRoutingPathfinder.cpp
    src/WallThicknessEnforcer.cpp
)

add_executable(RandomWalkRoomGeneratorTests
    tests/RandomWalkRoomGeneratorTests.cpp
    src/RandomWalkRoomGenerator.cpp
    src/WallThicknessEnforcer.cpp
)

add_executable(PoissonDiskSamplerTests
    tests/PoissonDiskSamplerTests.cpp
    src/PoissonDiskSampler.cpp
)

add_executable(MSTSolverTests
    tests/MSTSolverTests.cpp
    src/MSTSolver.cpp
    src/DisjointSet.cpp
)

add_executable(OrthogonalRoutingPathfinderTests
    tests/OrthogonalRoutingPathfinderTests.cpp
    src/OrthogonalRoutingPathfinder.cpp
)

add_executable(CoreGenerationTests
    tests/CoreGenerationTests.cpp
    src/AStarPathFinder.cpp
    src/DungeonGenerator.cpp
    src/RandomWalkRoomGenerator.cpp
    src/PoissonDiskSampler.cpp
    src/MSTSolver.cpp
    src/DisjointSet.cpp
    src/OrthogonalRoutingPathfinder.cpp
    src/WallThicknessEnforcer.cpp
    src/DungeonInterop.cpp
)

target_include_directories(QuickDungeonGeneration
    PRIVATE
        ${PROJECT_SOURCE_DIR}/include
)

target_include_directories(RandomWalkRoomGeneratorTests
    PRIVATE
        ${PROJECT_SOURCE_DIR}/include
)

target_include_directories(PoissonDiskSamplerTests
    PRIVATE
        ${PROJECT_SOURCE_DIR}/include
)

target_include_directories(MSTSolverTests
    PRIVATE
        ${PROJECT_SOURCE_DIR}/include
)

target_include_directories(OrthogonalRoutingPathfinderTests
    PRIVATE
        ${PROJECT_SOURCE_DIR}/include
)

target_include_directories(CoreGenerationTests
    PRIVATE
        ${PROJECT_SOURCE_DIR}/include
)

target_compile_features(QuickDungeonGeneration
    PRIVATE
        cxx_std_20
)

target_compile_features(RandomWalkRoomGeneratorTests
    PRIVATE
        cxx_std_20
)

target_compile_features(PoissonDiskSamplerTests
    PRIVATE
        cxx_std_20
)

target_compile_features(MSTSolverTests
    PRIVATE
        cxx_std_20
)

target_compile_features(OrthogonalRoutingPathfinderTests
    PRIVATE
        cxx_std_20
)

target_compile_features(CoreGenerationTests
    PRIVATE
        cxx_std_20
)

enable_testing()
add_test(NAME RandomWalkRoomGeneratorTests COMMAND RandomWalkRoomGeneratorTests)
add_test(NAME PoissonDiskSamplerTests COMMAND PoissonDiskSamplerTests)
add_test(NAME MSTSolverTests COMMAND MSTSolverTests)
add_test(NAME OrthogonalRoutingPathfinderTests COMMAND OrthogonalRoutingPathfinderTests)
add_test(NAME CoreGenerationTests COMMAND CoreGenerationTests)