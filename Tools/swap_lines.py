import os
import traceback

testdata_dir_name = "testdata.txt"

cwd = os.getcwd()
directories_list = [os.path.join(cwd, dir) for dir in os.listdir(cwd) if os.path.isdir(os.path.join(cwd, dir))]

def SwapLines(filepath):
    file = open(filepath, "r")
    file_contents = file.readlines()
    file.close()
	
    print(file_contents)
    line0 = file_contents[0].replace("п»ї", "")
    line1 = file_contents[1].replace("п»ї", "")
	
    if int(line0) < int(line1):
        print("true")
        width = file_contents[1]
        file_contents[1] = file_contents[0]
        file_contents[0] = width
        file = open(filepath, "w")
        file.writelines(file_contents)

for directory in directories_list:
    files = [os.path.join(directory, file) for file in os.listdir(directory) if (os.path.isfile(os.path.join(directory, file)))]
    testdata_file, = (file for file in files if "testdata.txt" in file)
    print(testdata_file)	
    SwapLines(testdata_file)