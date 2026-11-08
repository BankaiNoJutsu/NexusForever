#!/bin/bash
# usage: gap7_sheet.sh <file-fragment> <start_s> <step_s> <outprefix> [cols rows] [width]
FF="C:/Users/Bankai/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-8.1.1-full_build/bin/ffmpeg.exe"
cd /i/Videos/_Additional || exit 1
F=$(ls *"$1"*.mp4 | head -1)
f="I:/Videos/_Additional/$F"
C=${5:-4}; R=${6:-3}; W=${7:-480}
"$FF" -hide_banner -loglevel error -y -ss "$2" -i "$f" -vf "fps=1/$3,scale=$W:-1,tile=${C}x${R}" -frames:v 1 "$4.jpg"
