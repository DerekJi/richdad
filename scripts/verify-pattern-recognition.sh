#!/bin/bash
# Issue #7 Pattern Recognition Verification Script
# Quick verification of Al Brooks pattern recognition engine

echo "=== Al Brooks Pattern Recognition Verification ==="
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}Step 1: Trigger pattern recognition processing...${NC}"
curl -s -X POST "http://localhost:5000/api/pattern/process?symbol=XAUUSD&timeFrame=M5" | python -m json.tool
echo ""

echo -e "${YELLOW}Waiting 3 seconds for processing...${NC}"
sleep 3
echo ""

echo -e "${YELLOW}Step 2: Check statistics...${NC}"
curl -s "http://localhost:5000/api/pattern/stats?symbol=XAUUSD&timeFrame=M5" | python -m json.tool
echo ""

echo -e "${YELLOW}Step 3: Get latest 5 records (checking for Breakout patterns)...${NC}"
DATA=$(curl -s "http://localhost:5000/api/pattern/processed?symbol=XAUUSD&timeFrame=M5&count=5")
echo "$DATA" | python -m json.tool

# Check if we have breakout patterns
if echo "$DATA" | grep -q "\"BO\""; then
    echo ""
    echo -e "${GREEN}✓ Breakout patterns detected!${NC}"
else
    echo ""
    echo "No breakout patterns in latest 5 records (this is normal)"
fi
echo ""

echo -e "${YELLOW}Step 4: Test Markdown output...${NC}"
curl -s "http://localhost:5000/api/pattern/markdown?symbol=XAUUSD&timeFrame=M5&count=3" | python -c "import sys, json; data=json.load(sys.stdin); print(data['markdown'])"
echo ""

echo -e "${GREEN}=== Verification Complete ===${NC}"
