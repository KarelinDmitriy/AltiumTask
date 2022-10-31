For generate file of size <size> (in bytes) run application with command line arguments:
```-g <path_to_file> <size>```

Example: ```-g 1048576 C:/data/randomfile.txt```

For sort file run application with command line argumets:
```-s <path_to_file> <path_to_result> [thread_count] [init_blocks_size]```

Example: ```-s C:\data\randomfile.txt С:\data\sorted.txt 4 1048576```

- [thread_count] - optional, 8 by default
- [init_blocks_size] - optional, 128Kb by default

Merge sort algorithm.
Execution time avg.

Size|AMD Ryzen 5 5600U 2.30 GHz|AMD Ryzen 7 5800H 3.20 GHz|
| :---:   | :---: | :---: |
1Gb |1:20|0:43|
10Gb|13:30|9:35|

SSD memory usege: n * 3 (original file + splited blocks + merged block). 

