# GridModelControl
调度控制降雨切片数据生成和 网格模型的启动.

1、-updateraintile true   更新各个单元中的降雨路径

2、isgenraintile true  是否执行调用python降雨切片



3、-updatebyfile true 根据指定的模板文件更新exec.bat

模板文件标准3行



4、isstartbat false  是否启动bat 所有的单元



5、 -isCalcPerRegion true  屏蔽其他参数，启动单个exec。bat通过进程记录等待



6、 isshowchildprocess true 是否显示被管理的子进程cmd窗口

7、isSingleCC   边切降雨边计算的话，需要单个启动的时候更新写出execsingle.bat

8、-method wata \ province 按流域还是省份执行计算，流域的话使用多个computernode

9、-processnum  30    最多一次启动的进程个数。只有在当前查出的个数大于这个数值才生效张翔:
10、GridControl.exe.config文件中新增了一个配置项，如下：
    <!--//CSVLog-->
      <add key="CSVLogPath" value="\\192.168.100.100\s1-cpfs1\GridControlLog" />
表示会把程序计算时间统计起来，输出到这个文件夹下面



11、isUpdateParams是否根据数据库表更新参数值，包括土壤质地、土地利用对应的曼宁系数及渗水率等
12、isUpdateRivlParams更新河道宽度参数
12、历史命令
-method wata -isUpdateParams true

13、dat文件如果过大，会超过c#数组内存分配，没办法一次读取所有，然后执行切片，就要一次读取一个时间的栅格数据，当前时间切片完成后再执行下一个时间

-tilemehtod (all | one)  all代表一次加载所有，one代表一次处理一个