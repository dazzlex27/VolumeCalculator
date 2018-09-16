import os
import traceback

testdata_dir_name = "testdata.txt"

cwd = os.getcwd()
directories_list = [os.path.join(cwd, dir) for dir in os.listdir(cwd) if os.path.isdir(os.path.join(cwd, dir))]

def SwapLengthWidth(filepath):
    file = open(filepath, "r")
    file_contents = file.readlines()
    file.close()

    line0 = file_contents[0].replace("п»ї", "")
    line1 = file_contents[1].replace("п»ї", "")

    if int(line0) < int(line1):
        print("swapped length & width")
        width = file_contents[1]
        file_contents[1] = file_contents[0]
        file_contents[0] = width
        file = open(filepath, "w")
        file.writelines(file_contents)
        file.close()
	
def SwapWidthHeight(filepath):
    file = open(filepath, "r")
    file_contents = file.readlines()
    file.close()
    
    temp_value = file_contents[1]
    file_contents[1] = file_contents[2]
    file_contents[2] = temp_value
    file = open(filepath, "w")
    file.writelines(file_contents)
    file.close()

for directory in directories_list:
    print(directory)
    files = [os.path.join(directory, file) for file in os.listdir(directory) if (os.path.isfile(os.path.join(directory, file)))]
    if len(files) == 0:
        continue;

    testdata_file, = (file for file in files if "testdata.txt" in file)
    SwapWidthHeight(testdata_file)
    SwapLengthWidth(testdata_file)