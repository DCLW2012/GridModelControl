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



11、isUpdateParams是否根据数据库表更新参数值，包括土壤质地、土地利用对应的曼宁系数及渗水率等   --- 已废弃
12、isUpdateRivlParams更新河道宽度参数---  已废弃
12、历史命令
-method wata -isUpdateParams true

//按省份计算岚山风险普查区域

GridControl -method province -processnum 32 -isGridout true -province shandong   -按省份计算

//按省份计算海南，台风验证

GridControl -method province -processnum 32 -isGridout true -province hainan



//查询省份下某个uinit信息

SELECT DISTINCT
        T1.UNITCD,
        T2.ComputeNode,
        T2.APPPath
FROM
        GRID_HSFX_UNIT T1
LEFT JOIN HSFX_ComputeUnit T2 ON T1.GroupID = T2.ComputeUnit
WHERE
         T1.UNITCD IN (
        'whf60_1_5'
)