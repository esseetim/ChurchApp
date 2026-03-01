#!/bin/zsh

set -euo pipefail

# Test script for ChurchApp API
echo "🧪 Testing ChurchApp API Setup"
echo "==============================="
echo ""

SCRIPT_DIR="${0:A:h}"
cd "$SCRIPT_DIR"

# Ensure compiled model is generated before any build/run step
./ensure-ef-compiled-model.sh

# Clean and build
echo "🧹 Cleaning solution..."
dotnet clean --verbosity quiet
echo "✅ Clean complete"
echo ""

echo "📦 Restoring packages..."
dotnet restore --source https://api.nuget.org/v3/index.json --verbosity quiet
echo "✅ Restore complete"
echo ""

echo "🔨 Building solution..."
BUILD_OUTPUT=$(dotnet build --no-restore --verbosity normal 2>&1)
BUILD_EXIT=$?

if [ $BUILD_EXIT -eq 0 ]; then
    echo "✅ Build successful!"
    echo ""
    echo "📁 Checking output files..."
    if [ -f "ChurchApp.API/ChurchApp.API/bin/Debug/net10.0/ChurchApp.API.dll" ]; then
        echo "✅ ChurchApp.API.dll found"
    else
        echo "❌ ChurchApp.API.dll not found"
    fi
    
    if [ -f "ChurchApp.Application/ChurchApp.Application/bin/Debug/net10.0/ChurchApp.Application.dll" ]; then
        echo "✅ ChurchApp.Application.dll found"
    else
        echo "❌ ChurchApp.Application.dll not found"
    fi
    echo ""
    
    echo "🚀 Starting application..."
    cd ChurchApp.API/ChurchApp.API
    
    # Start the app in background
    dotnet run --no-build > /tmp/churchapp.log 2>&1 &
    APP_PID=$!
    echo "Application started with PID: $APP_PID"
    
    # Wait for startup
    echo "⏳ Waiting for application to start..."
    sleep 5
    
    # Test the health endpoint
    echo "🏥 Testing health endpoint..."
    HEALTH_RESPONSE=$(curl -s http://localhost:5000/health 2>&1)
    CURL_EXIT=$?
    
    if [ $CURL_EXIT -eq 0 ]; then
        echo "✅ Health endpoint responded:"
        echo "$HEALTH_RESPONSE"
    else
        echo "❌ Failed to connect to health endpoint"
        echo "Checking application log:"
        cat /tmp/churchapp.log
    fi
    
    # Stop the app
    echo ""
    echo "🛑 Stopping application..."
    kill $APP_PID 2>/dev/null
    
else
    echo "❌ Build failed!"
    echo "$BUILD_OUTPUT"
fi

