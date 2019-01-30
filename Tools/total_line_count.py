import os
import traceback

dir_list = (
"D435FrameProvider",
"DeviceIntegration",
"ExtIntegration",
"FrameProviders",
"FrameProcessor",
"libDepthMapProcessor",
"Primitives",
"Tests/VolumeCalculatorTests", 
"Tests/DepthMapProcessorTests", 
"Tests/VolumeCalculatorTests",
"Utils/MacReader",
"Utils/VersionWriter",
"VolumeCalculatorGUI")
ext_list = ("h", "cpp", "c", "cs")

total_count = 0

print("Line count by project:")
print("===============")
try:
    for directory in dir_list:
        count = 0
        for (dir,_, files) in os.walk(os.path.join("..", directory)):
                for f in files:
                    path = os.path.join(dir, f)

                    file_is_ok = os.path.exists(path) and path.endswith(ext_list)
                    file_is_service = path.endswith("g.i.cs") or "\\obj\\" in path
                    if file_is_ok and not file_is_service:
                        count += len(open(path, "rt", encoding="utf-8").readlines(  ))
        total_count += count
        print(directory + ": ", count)
    print("===============")
    print("Total count = ", total_count)
except:
    traceback.print_exc()