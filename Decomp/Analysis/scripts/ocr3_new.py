r"""ocr3: copy the OCR result files written by the pass-3 sweep (jsonl newer than a given time) into I:\tools\ocr3\new_jsonl so
ocr2_analyze.py --dir can analyse only the new windows.
  py -3.13 -I ocr3_new.py <unix seconds>   (default: files modified in the last 3 hours)
then: py -3.13 -I ocr2_analyze.py --dir I:\tools\ocr3\new_jsonl --min-frames 3 --csv I:\tools\ocr3\clusters_new.csv"""
import glob, os, shutil, sys, time
SRC = r"I:\tools\video_index\ocr2"
DST = r"I:\tools\ocr3\new_jsonl"
since = float(sys.argv[1]) if len(sys.argv) > 1 else time.time() - 3 * 3600
shutil.rmtree(DST, ignore_errors=True)
os.makedirs(DST)
n = 0
for fn in glob.glob(os.path.join(SRC, "*.jsonl")):
    if os.path.getmtime(fn) >= since:
        shutil.copy(fn, DST)
        n += 1
print(n, "files copied")
