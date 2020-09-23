# GridModelControl
调度控制降雨切片数据生成和 网格模型的启动

1、-updateraintile true   更新各个单元中的降雨路径

2、isgenraintile true  是否执行调用python降雨切片



3、-updatebyfile true 根据指定的模板文件更新exec.bat

模板文件标准3行



4、isstartbat false  是否启动bat 所有的单元



5、 -isCalcPerRegion true  屏蔽其他参数，启动单个exec。bat通过进程记录等待



6、 isshowcmd true 是否显示cmd串口

7、isSingleCC   边切降雨边计算的话，需要单个启动的时候更新写出execsingle.bat