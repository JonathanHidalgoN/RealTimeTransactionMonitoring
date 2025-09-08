#!/bin/bash

# Colors for output
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Available test categories
AVAILABLE_CATEGORIES=("Infrastructure" "API" "E2E" "Smoke")

show_help() {
    echo -e "${YELLOW}======================================================${NC}"
    echo -e "${YELLOW}  Integration Test Runner${NC}"
    echo -e "${YELLOW}======================================================${NC}"
    echo ""
    echo "Usage: container-test-runner.sh [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -c, --category CATEGORY    Run tests in specific category"
    echo "  -v, --verbosity LEVEL      Test verbosity (minimal, normal, detailed)"
    echo "  -l, --list                 List available test categories"
    echo "  -h, --help                 Show this help message"
    echo ""
    echo -e "${CYAN}Available categories:${NC}"
    for category in "${AVAILABLE_CATEGORIES[@]}"; do
        echo -e "  ${GREEN}- $category${NC}"
    done
    echo ""
    echo -e "${CYAN}Examples:${NC}"
    echo "  container-test-runner.sh                     # Run all tests"
    echo "  container-test-runner.sh -c Infrastructure   # Run Infrastructure tests"
    echo "  container-test-runner.sh -c API -v detailed  # Run API tests with detailed output"
}

list_categories() {
    echo -e "${CYAN}Available test categories:${NC}"
    for category in "${AVAILABLE_CATEGORIES[@]}"; do
        echo -e "  ${GREEN}- $category${NC}"
    done
}

validate_category() {
    local category="$1"
    for valid_category in "${AVAILABLE_CATEGORIES[@]}"; do
        if [[ "$valid_category" == "$category" ]]; then
            return 0
        fi
    done
    return 1
}

TEST_CATEGORY=""
TEST_VERBOSITY="minimal"

while [[ $# -gt 0 ]]; do
    case $1 in
    -c | --category)
        TEST_CATEGORY="$2"
        shift 2
        ;;
    -v | --verbosity)
        TEST_VERBOSITY="$2"
        shift 2
        ;;
    -l | --list)
        list_categories
        exit 0
        ;;
    -h | --help)
        show_help
        exit 0
        ;;
    *)
        echo "Unknown option: $1"
        echo "Use -h or --help for usage information"
        exit 1
        ;;
    esac
done

# Fallback to environment variable
if [[ -z "$TEST_CATEGORY" ]]; then
    TEST_CATEGORY=${TEST_CATEGORY:-""}
fi

if [[ -n "$TEST_CATEGORY" ]] && ! validate_category "$TEST_CATEGORY"; then
    echo -e "${YELLOW}Error: Invalid category '$TEST_CATEGORY'${NC}"
    echo ""
    list_categories
    exit 1
fi

cd /source

mkdir -p /app/test-results

if [[ -n "$TEST_CATEGORY" ]]; then
    echo -e "${CYAN}Running tests with category filter: ${GREEN}$TEST_CATEGORY${NC}"
    FILTER_ARG="--filter Category=$TEST_CATEGORY"
else
    echo -e "${CYAN}Running all integration tests${NC}"
    FILTER_ARG=""
fi

echo ""
echo -e "${YELLOW}--- Discovering Tests ---${NC}"
dotnet test tests/FinancialMonitoring.IntegrationTests/FinancialMonitoring.IntegrationTests.csproj --list-tests --no-build -c Release

echo ""
echo -e "${YELLOW}--- Executing Tests ---${NC}"
echo -e "${CYAN}Command: dotnet test tests/FinancialMonitoring.IntegrationTests/FinancialMonitoring.IntegrationTests.csproj --no-build -c Release --logger trx --results-directory /app/test-results --verbosity $TEST_VERBOSITY $FILTER_ARG${NC}"
echo ""

dotnet test tests/FinancialMonitoring.IntegrationTests/FinancialMonitoring.IntegrationTests.csproj \
    --no-build \
    -c Release \
    --logger trx \
    --results-directory /app/test-results \
    --verbosity $TEST_VERBOSITY \
    $FILTER_ARG

