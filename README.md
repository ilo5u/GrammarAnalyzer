# GrammarAnalyzer
对于给定文法，自动生成LL(1)、SLR(1)、LR(1)分析表

This app can automatically generate analysis sheet of specific grammar.e.g. LL(1), SLR(1) and LR(1). Mainly used for those clumsy questions in courses like *Principles of Compiling*.

该UWP应用上架已至Windows应用商店 —— 检索“语法分析器”即可

You can easily get this app on *Windows App Store* by typing "语法分析器" to search and install it, like this,

<img src="https://github.com/ilo5u/GrammarAnalyzer/tree/master/Pics/search.png">

代码整体分为三部分：

1. 前端C#编写交互响应

2. 中端C++/Cli做C#和C++对接

3. 后端C++编写文法处理的内核

The whole project including parts as,

1. Front-end in C# cause UWP required
2. Backend in C++ for kernel processing grammar analysis
3. Mid-layer in C++/Cli for enabling the C# to communicate with C++

# Preface
在学习编译原理课程中的语法分析内容时，必须要掌握对上下文无关文法构造其对应的 LL(1)、 SLR(1)以及 LR(1)分析表以及分析程序的方法。该应用能帮助使用者自动构造确定文法的 LL(1)、 SLR(1)以及 LR(1)分析程序，极大地节省了手动构造所带来的时间开销，并且也能帮助使用者判断 其手动构造的分析表是否正确。

In *Principle of Compiling* course, how to build the analysis sheet and simulate the analysis process are essential but clumsy. I do not think the costing time is valuable, thus this app appears.

# Guide

## Tool Bar

<img src="https://github.com/ilo5u/GrammarAnalyzer/tree/master/Pics/home.png" style="zoom:50%;" />

The left *tool bar* has tow buttons, the upper one guides to the home page as the above shows, and the reset one to *help* page, where you can access the web [link](https://github.com/ilo5u/GrammarAnalyzer) to the *README.md* if you forget how to use the app.

## Import



后续有空再进行完善

