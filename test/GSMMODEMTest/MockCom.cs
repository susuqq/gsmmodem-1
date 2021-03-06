﻿using System;
using System.Collections.Generic;
using Moq;
using GSMMODEM;
using System.IO;

namespace GSMMODEMTest
{
    class MockCom
    {
        private Mock<ICom> mockCom;

        public Mock<ICom> MockObject
        {
            get { return mockCom; }
        }  

        //mock串口 接收缓冲区
        string recieveBuffer;

        public MockCom()
        {
            mockCom = new Mock<ICom>();           //串口mock对象

            //串口写数据 调用串口发送接收数据处理
            mockCom.Setup(com => com.Write(It.IsAny<String>())).Callback<String>(s => RTHandling(s));

            //串口设备打开 IsOpen属性改为true
            mockCom.Setup(com => com.Open()).Callback(() => { mockCom.Setup(com => com.IsOpen).Returns(true); });

            //串口设备关闭 IsOpen属性改为fasle
            mockCom.Setup(com => com.Close()).Callback(() => { mockCom.Setup(com => com.IsOpen).Returns(false); });

            //mock串口 从缓冲区读数据
            mockCom.Setup(com => com.ReadTo(It.IsAny<string>())).Returns<string>(
                s =>
                {
                    int index = recieveBuffer.IndexOf(s);
                    if (index == -1)
                    {
                        throw new Exception("index==-1");
                    }
                    string result = recieveBuffer.Substring(0, index);
                    recieveBuffer = recieveBuffer.Remove(0, index + s.Length);
                    return result;
                });
            mockCom.Setup(com => com.ReadLine()).Returns(
                () =>
                {
                    return mockCom.Object.ReadTo("\n");
                });
            mockCom.Setup(com => com.ReadExisting()).Returns(() =>
            {
                string result = recieveBuffer; 
                recieveBuffer = string.Empty;
                return result;
            });
        }

        /// <summary>
        /// 从文件中读取串口 发送和对应返回列表
        /// </summary>
        /// <returns>每行字符串返回</returns>
        private List<string> ReadLineFromTxt()
        {
            List<string> result = new List<string>();
            string line = string.Empty;

            using (StreamReader sr = new StreamReader("MockSerialPortDat.txt"))
            {
                while (sr.Peek() >= 0)
                {
                    line = sr.ReadLine();
                    if (line[0] == '#')
                    {
                        continue;   //注释 跳过
                    }
                    //返回时 \r \n翻译为对应字符
                    result.Add(line.Replace(@"\r", "\r").Replace(@"\n", "\n").Replace(@"^Z", ((char)(26)).ToString()));
                }
            }

            return result;
        }

        /// <summary>
        /// 串口写数据后 设置串口应返回数据 通过文本文档读取应返回值
        /// </summary>
        /// <param name="s">串口写入数据内容</param>
        private void RTHandling(String s)
        {
            foreach (string st in ReadLineFromTxt())
            {
                if (st.Split(';')[0].Contains(s))
                {
                        recieveBuffer += (st.Split(';')[1]);
                }
            }
        }

        public void SmsRecieved()
        {
            recieveBuffer = "+CMTI: \"SM\",1\r\n";
            this.mockCom.Raise(com => com.DataReceived += null,new EventArgs());
        }

    }
}
