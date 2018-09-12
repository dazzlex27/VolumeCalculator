import os
import traceback

map_dir_name = "maps"
floor_filename = "floor.txt"

cwd = os.getcwd()
directories_list = [os.path.join(cwd, dir) for dir in os.listdir(cwd) if os.path.isdir(os.path.join(cwd, dir))]

def MoveMapsToSepFolder(directory, files):
    dm_files = [file for file in files if file.endswith('.dm')]
    map_dir_fullname = os.path.join(directory, map_dir_name)
    if not os.path.exists(map_dir_fullname):
        os.mkdir(map_dir_fullname)
		
    for file in dm_files:
        os.rename(file, os.path.join(map_dir_fullname, os.path.basename(file)))
    print("moved", len(dm_files), "dm files")	

for directory in directories_list:
    print(directory)
    files = [os.path.join(directory, file) for file in os.listdir(directory) if (os.path.isfile(os.path.join(directory, file)))]
    MoveMapsToSepFolder(directory, files)
    floor_full_filename = os.path.join(directory, floor_filename)
    if os.path.exists(floor_full_filename):
        os.remove(floor_full_filename)


