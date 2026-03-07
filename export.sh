#!/bin/bash
set -e

UNITY="/Applications/Unity/Hub/Editor/6000.3.6f1/Unity.app/Contents/MacOS/Unity"
PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"
LOG="$PROJECT_DIR/unity_export.log"

echo "Exporting package..."
"$UNITY" \
  -batchmode \
  -quit \
  -projectPath "$PROJECT_DIR" \
  -executeMethod PackageExporter.ExportBatch \
  -logFile "$LOG"

echo "Done: $PROJECT_DIR/ProceduralRooms.unitypackage"
