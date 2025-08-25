#!/bin/bash
# Purpose: Complete setup script for local k3d development environment
# Orchestrates cluster creation, image building, external dependencies, and deployment

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
RED='\033[0;31m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

print_usage() {
    echo "Usage: $0 [COMMAND]"
    echo ""
    echo "Commands:"
    echo "  all           Run complete setup (default)"
    echo "  cluster       Create k3d cluster only"
    echo "  build         Build Docker images only"
    echo "  import        Import images to k3d only"
    echo "  compose       Start Docker Compose dependencies only"
    echo "  apply         Apply Kubernetes manifests only"
    echo "  teardown      Stop and remove everything"
    echo "  status        Show status of all components"
    echo ""
    echo "Examples:"
    echo "  $0              # Run complete setup"
    echo "  $0 all          # Run complete setup"
    echo "  $0 cluster      # Just create k3d cluster"
    echo "  $0 compose      # Just start Kafka/MongoDB"
    echo "  $0 teardown     # Clean everything up"
}

print_header() {
    echo -e "${CYAN}======================================================${NC}"
    echo -e "${CYAN}  $1"
    echo -e "${CYAN}======================================================${NC}"
    echo ""
}

create_cluster() {
    print_header "Step 1: Creating k3d Cluster"
    echo -e "${YELLOW}Running: ${SCRIPT_DIR}/create-k3d-cluster.sh${NC}"
    "$SCRIPT_DIR/create-k3d-cluster.sh"
    echo -e "${GREEN}✓ k3d cluster created successfully${NC}"
    echo ""
}

build_images() {
    print_header "Step 2: Building Docker Images"
    echo -e "${YELLOW}Running: ${SCRIPT_DIR}/build-images-local-development.sh${NC}"
    "$SCRIPT_DIR/build-images-local-development.sh"
    echo -e "${GREEN}✓ Docker images built successfully${NC}"
    echo ""
}

import_images() {
    print_header "Step 3: Importing Images to k3d"
    echo -e "${YELLOW}Running: ${SCRIPT_DIR}/import-k3d-images.sh${NC}"
    "$SCRIPT_DIR/import-k3d-images.sh"
    echo -e "${GREEN}✓ Images imported to k3d successfully${NC}"
    echo ""
}

start_compose() {
    print_header "Step 4: Starting External Dependencies (Kafka + MongoDB)"
    echo -e "${YELLOW}Starting Docker Compose for external dependencies...${NC}"
    cd "$PROJECT_ROOT"
    
    if docker compose -f docker-compose-k3d-local.yml ps | grep -q "running"; then
        echo -e "${YELLOW}Dependencies already running, restarting...${NC}"
        docker compose -f docker-compose-k3d-local.yml down
    fi
    
    docker compose -f docker-compose-k3d-local.yml up -d
    
    echo -e "${YELLOW}Waiting for services to be healthy...${NC}"
    sleep 10
    
    # Wait for Kafka to be ready
    local max_attempts=30
    local attempt=1
    while [ $attempt -le $max_attempts ]; do
        if docker compose -f docker-compose-k3d-local.yml exec -T kafka kafka-broker-api-versions --bootstrap-server localhost:9092 &>/dev/null; then
            echo -e "${GREEN}✓ Kafka is ready${NC}"
            break
        fi
        echo -e "${YELLOW}Waiting for Kafka... (attempt $attempt/$max_attempts)${NC}"
        sleep 5
        ((attempt++))
    done
    
    if [ $attempt -gt $max_attempts ]; then
        echo -e "${RED}✗ Kafka failed to start within expected time${NC}"
        exit 1
    fi
    
    echo -e "${GREEN}✓ External dependencies are running${NC}"
    echo ""
}

apply_manifests() {
    print_header "Step 5: Applying Kubernetes Manifests"
    echo -e "${YELLOW}Running: ${SCRIPT_DIR}/apply-k8s-individual.sh${NC}"
    "$SCRIPT_DIR/apply-k8s-individual.sh"
    echo -e "${GREEN}✓ Kubernetes manifests applied successfully${NC}"
    echo ""
}

show_status() {
    print_header "Environment Status"
    
    echo -e "${CYAN}k3d Cluster:${NC}"
    if k3d cluster list | grep -q "finmon-local"; then
        k3d cluster list | grep finmon-local || true
    else
        echo -e "${RED}  No k3d cluster found${NC}"
    fi
    echo ""
    
    echo -e "${CYAN}Docker Compose Services:${NC}"
    cd "$PROJECT_ROOT"
    if [ -f docker-compose-k3d-local.yml ]; then
        docker compose -f docker-compose-k3d-local.yml ps || echo -e "${RED}  No compose services running${NC}"
    fi
    echo ""
    
    echo -e "${CYAN}Kubernetes Pods:${NC}"
    if kubectl get pods -n finmon-app 2>/dev/null; then
        true
    else
        echo -e "${RED}  No pods found or cluster not accessible${NC}"
    fi
    echo ""
    
    echo -e "${CYAN}Port Forwarding Info:${NC}"
    echo -e "  • Kafka: localhost:9092"
    echo -e "  • MongoDB: localhost:27017"
    echo -e "  • API: kubectl port-forward svc/api-service 5100:80 -n finmon-app"
    echo ""
}

teardown_environment() {
    print_header "Tearing Down Environment"
    
    echo -e "${YELLOW}Stopping Docker Compose services...${NC}"
    cd "$PROJECT_ROOT"
    docker compose -f docker-compose-k3d-local.yml down -v 2>/dev/null || true
    
    echo -e "${YELLOW}Deleting k3d cluster...${NC}"
    k3d cluster delete finmon-local 2>/dev/null || true
    
    echo -e "${GREEN}✓ Environment cleaned up${NC}"
    echo ""
}

run_complete_setup() {
    print_header "Complete Local k3d Development Setup"
    echo -e "${YELLOW}This will set up your complete local development environment${NC}"
    echo ""
    
    create_cluster
    build_images
    import_images
    start_compose
    apply_manifests
    
    print_header "Setup Complete!"
    echo -e "${GREEN}Your local k3d development environment is ready!${NC}"
    echo ""
    echo -e "${CYAN}Next steps:${NC}"
    echo -e "  1. Check pod status: kubectl get pods -n finmon-app"
    echo -e "  2. View logs: kubectl logs -f deployment/api-deployment -n finmon-app"
    echo -e "  3. Port forward API: kubectl port-forward svc/api-service 5100:80 -n finmon-app"
    echo -e "  4. Access API: http://localhost:5100/health"
    echo ""
    echo -e "${YELLOW}To tear down: $0 teardown${NC}"
}

# Main script logic
case "${1:-all}" in
    "all")
        run_complete_setup
        ;;
    "cluster")
        create_cluster
        ;;
    "build")
        build_images
        ;;
    "import")
        import_images
        ;;
    "compose")
        start_compose
        ;;
    "apply")
        apply_manifests
        ;;
    "status")
        show_status
        ;;
    "teardown")
        teardown_environment
        ;;
    "help"|"-h"|"--help")
        print_usage
        ;;
    *)
        echo -e "${RED}Unknown command: $1${NC}"
        echo ""
        print_usage
        exit 1
        ;;
esac