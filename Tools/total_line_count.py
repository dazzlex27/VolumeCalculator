import os
import traceback

root_dir = '../'
excluded_dirs = ['.git', '.vs']
ext_list = ("h", "cpp", "c", "cs")

total_count = 0

try:
    for (dir,_, files) in os.walk(root_dir):
        contains_exc_dir = any(ed in dir for ed in excluded_dirs)
        if contains_exc_dir:
            continue
            
        for f in files:
            path = os.path.join(dir, f)

            file_is_ok = os.path.exists(path) and path.endswith(ext_list)
            file_is_service = path.endswith("g.i.cs") or "\\obj\\" in path
            if file_is_ok and not file_is_service:
                total_count += len(open(path, "rt", encoding="utf-8").readlines(  ))
    print("Total count = ", total_count)
except:
    traceback.print_exc()