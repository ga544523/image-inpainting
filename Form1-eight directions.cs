using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.Win32;
using System.Collections.Concurrent;
using Emgu.CV.CvEnum;
using Emgu.CV.BgSegm;
using Emgu.CV.Ocl;
using System.Globalization;


namespace imageinpaint
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public int leftx, lefty;
        public int rightx, righty;
        public int col, row;
        public int blocksize = 9;
        Image<Bgr, Byte> oriimage;
        Image<Bgr, Byte> inputimage;
        int[,] vis = new int[555, 555];
        int[,] inqueue = new int[555, 555];
        double[,] confidence = new double[555, 555];

        List<List<Tuple<int, int, int>>> block;
        public List<Tuple<int, int>> getboundary() {
            List<Tuple<int, int>> boundary = new List<Tuple<int, int>>();
            Queue<Tuple<int, int>> q = new Queue<Tuple<int, int>>();
            q.Enqueue(new Tuple<int, int>(0, 0));
            for (int i = 0; i < 555; i++) {
                for (int j = 0; j < 555; j++) {
                    vis[i, j] = 0;
                    inqueue[i, j] = 0;
                }
            }

            vis[0, 0] = 1;

            int[] di = { 0, 1, -1, 0 };
            int[] dj = { 1, 0, 0, -1 };
            while (q.Count > 0) {

                Tuple<int, int> now = q.Dequeue();

                for (int i = 0; i < 4; i++) {
                    int t1 = now.Item1 + di[i], t2 = now.Item2 + dj[i];
                    if (t1 < 0 || t1 >= row || t2 < 0 || t2 >= col || vis[t1, t2] == 1) {
                        continue;
                    }

                    if (inputimage.Data[t1, t2, 0] == 0 && inputimage.Data[t1, t2, 1] == 0 && inputimage.Data[t1, t2, 2] == 0 && vis[t1, t2] == 0) {
                        if (inqueue[now.Item1, now.Item2] == 0)
                        {
                            inqueue[now.Item1, now.Item2] = 1;
                            boundary.Add(new Tuple<int, int>(now.Item1, now.Item2));

                        }
                        continue;
                    }

                    vis[t1, t2] = 1;
                    q.Enqueue(new Tuple<int, int>(t1, t2));

                }

            }
            return boundary;

        }

        double cmpsimilar(List<Tuple<int, int, int>>pos, List<Tuple<int, int, int>> blo ) {

            double sum = 0;
            for (int i = 0; i < pos.Count; i++) {
                if (pos[i].Item1 == 0 && pos[i].Item2 == 0 && pos[i].Item3 == 0)
                    continue;

                sum += 1.0 * (pos[i].Item1 - blo[i].Item1) * (pos[i].Item1 - blo[i].Item1);
                sum += 1.0 * (pos[i].Item2 - blo[i].Item2) * (pos[i].Item2 - blo[i].Item2);
                sum += 1.0 * (pos[i].Item3 - blo[i].Item3) * (pos[i].Item3 - blo[i].Item3);
            }
            return sum;
        }

        int getsimilar(Tuple<int,int>pos){

            List<Tuple<int, int, int>> tmp = new List<Tuple<int, int, int>>();
            for (int j = pos.Item1 - blocksize / 2; j <= pos.Item1 + blocksize / 2; j++)
            {
                for (int k = pos.Item2 - blocksize / 2; k <= pos.Item2 + blocksize / 2; k++)
                {
                    if (j < 0 || j >= row || k < 0 || k >= col)
                    {
                        continue;
                    }
                    if (inputimage.Data[j, k, 0] > 0 || inputimage.Data[j, k, 1] > 0 || inputimage.Data[j, k, 2] > 0)
                    {
                        tmp.Add(new Tuple<int, int, int>(inputimage.Data[j, k, 0],
                    inputimage.Data[j, k, 1],
                    inputimage.Data[j, k, 2]));
                    }

                    else
                    {
                        tmp.Add(new Tuple<int, int, int>(0, 0, 0));
                    }
                }

            }


            double minr = 9999999;
            int minid = 0;
            for (int i = 0; i < block.Count; i++) {

                double num = cmpsimilar(tmp,block[i]);
                if (num < minr) {
                    minr = num;
                    minid = i;
            }

            }

          
            return minid;
            

        }
        void draw(int patch,Tuple<int,int>pos,double con) {
            int cnt = 0;
            List<Tuple<int, int, int>> newblock = new List<Tuple<int, int, int>>();
            for (int j = pos.Item1 - blocksize / 2; j <= pos.Item1 + blocksize / 2; j++)
            {
                for (int k = pos.Item2 - blocksize / 2; k <= pos.Item2 + blocksize / 2; k++)
                {
                    
                    if (j < 0 || j >=row || k < 0 || k >= col)
                    {
                        continue;
                    }
                    if (inputimage.Data[j, k, 0] == 0 && inputimage.Data[j, k, 1] == 0 && inputimage.Data[j, k, 2] == 0)
                    {
                        confidence[j, k] = con;

                        inputimage.Data[j, k, 0] = (byte)block[patch][cnt].Item1;
                        inputimage.Data[j, k, 1] = (byte)block[patch][cnt].Item2;
                        inputimage.Data[j, k, 2] = (byte)block[patch][cnt].Item3;

                    }

                    newblock.Add(new Tuple<int, int, int>(inputimage.Data[j, k, 0], inputimage.Data[j, k, 1], inputimage.Data[j, k, 2]));

                    cnt++;
                }

            }
            //block.Add(newblock);
        }

        public void solve() {
            
            while (true)
            {
                Image<Gray, Byte> imggray = new Image<Gray, Byte>(col,row);

                for (int i = 0; i < row; i++) {
                    for (int j = 0; j < col; j++) {
                        imggray.Data[i, j,0] =(byte) ((inputimage.Data[i, j, 0]+ inputimage.Data[i, j, 1] + inputimage.Data[i, j, 2])/3) ;
                    }
                }

                List<Tuple<int, int>> boundary = getboundary();

                if (boundary.Count == 0) {
                    break;
                }

                //solve priority
                List<Tuple<double, int, int>> priority = new List<Tuple<double, int, int>>();

                for (int i = 0; i < boundary.Count; i++) {

                    //conpute gradient
                    int[] grad = new int[2];
                    if (boundary[i].Item1 - 1 >= 0 && (inputimage.Data[boundary[i].Item1 - 1, boundary[i].Item2, 0] > 0
                        || inputimage.Data[boundary[i].Item1 - 1, boundary[i].Item2, 1] > 0
                        || inputimage.Data[boundary[i].Item1 - 1, boundary[i].Item2, 2] > 0
                        )        ) 
                    {
                        grad[1] = imggray.Data[boundary[i].Item1, boundary[i].Item2, 0] -
                            imggray.Data[boundary[i ].Item1-1, boundary[i].Item2, 0];
                  
                    }
                    else if (boundary[i].Item1 + 1 < row && (inputimage.Data[boundary[i].Item1 + 1, boundary[i].Item2, 0] > 0
                        || inputimage.Data[boundary[i].Item1 + 1, boundary[i].Item2, 1] > 0
                        || inputimage.Data[boundary[i].Item1 + 1, boundary[i].Item2, 2] > 0
                        ) )
                    {
                        grad[1] = imggray.Data[boundary[i].Item1, boundary[i].Item2, 0] -
                            imggray.Data[boundary[i ].Item1+1, boundary[i].Item2, 0];
                    }
                    if (boundary[i].Item2 - 1 >= 0 && (inputimage.Data[boundary[i].Item1 , boundary[i].Item2-1, 0] > 0
                        || inputimage.Data[boundary[i].Item1 , boundary[i].Item2-1, 1] > 0
                        || inputimage.Data[boundary[i].Item1 , boundary[i].Item2-1, 2] > 0
                        ))
                    {
                        grad[0] = imggray.Data[boundary[i].Item1,boundary[i].Item2,0]
                            - imggray.Data[boundary[i].Item1, boundary[i].Item2-1, 0];
                    }
                    else if (boundary[i].Item2 + 1 < col && (inputimage.Data[boundary[i].Item1 , boundary[i].Item2+1, 0] > 0
                        || inputimage.Data[boundary[i].Item1 , boundary[i].Item2+1, 1] > 0
                        || inputimage.Data[boundary[i].Item1 , boundary[i].Item2+1, 2] > 0
                        ))
                    {
                        grad[0] = imggray.Data[boundary[i].Item1, boundary[i].Item2, 0]
                            - imggray.Data[boundary[i].Item1, boundary[i ].Item2+1, 0];
                    }
                    grad[0] *= -1;


                    //conpute normal vector
                    int []normalvec = new int[2];
                    if (boundary[i].Item1 - 1 >= 0&& inputimage.Data[boundary[i].Item1 - 1, boundary[i].Item2, 0] == 0 &&
                            inputimage.Data[boundary[i].Item1 - 1, boundary[i].Item2, 1] == 0 &&
                            inputimage.Data[boundary[i].Item1 - 1, boundary[i].Item2, 2] == 0) {
                            normalvec[1] = -1;
                        normalvec[0] = 0;
                        double D = Math.Abs(1.0 * (grad[0] * normalvec[0] + grad[1] * normalvec[1])) / 255.0;


                        double C = 0;

                        for (int j = boundary[i].Item1 - blocksize / 2; j <= boundary[i].Item1 + blocksize / 2; j++)
                        {
                            for (int k = boundary[i].Item2 - blocksize / 2; k <= boundary[i].Item2 + blocksize / 2; k++)
                            {
                                if (j < 0 || j >= row || k < 0 || k >= col)
                                {
                                    continue;
                                }
                                if (inputimage.Data[j, k, 0] > 0 || inputimage.Data[j, k, 1] > 0 || inputimage.Data[j, k, 2] > 0)
                                {

                                    C+=confidence[j,k];
                                }

                            }

                        }

                        C /= 1.0 * (blocksize * blocksize);

                        priority.Add(new Tuple<double, int, int>(C * D, boundary[i].Item1, boundary[i].Item2));
                    }

                    if (boundary[i].Item1 + 1 < row&& inputimage.Data[boundary[i].Item1 + 1, boundary[i].Item2, 0] == 0 &&
                            inputimage.Data[boundary[i].Item1 + 1, boundary[i].Item2, 1] == 0 &&
                            inputimage.Data[boundary[i].Item1 + 1, boundary[i].Item2, 2] == 0)
                    {

                            normalvec[1] = 1;
                        normalvec[0] = 0;
                        double D = Math.Abs(1.0 * (grad[0] * normalvec[0] + grad[1] * normalvec[1])) / 255.0;


                        double C = 0;

                        for (int j = boundary[i].Item1 - blocksize / 2; j <= boundary[i].Item1 + blocksize / 2; j++)
                        {
                            for (int k = boundary[i].Item2 - blocksize / 2; k <= boundary[i].Item2 + blocksize / 2; k++)
                            {
                                if (j < 0 || j >= row || k < 0 || k >= col)
                                {
                                    continue;
                                }
                                if (inputimage.Data[j, k, 0] > 0 || inputimage.Data[j, k, 1] > 0 || inputimage.Data[j, k, 2] > 0)
                                {

                                    C += confidence[j, k];
                                }

                            }

                        }

                        C /= 1.0 * (blocksize * blocksize);

                        priority.Add(new Tuple<double, int, int>(C * D, boundary[i].Item1, boundary[i].Item2));

                    }

                    if (boundary[i].Item2 - 1 >=0 && (inputimage.Data[boundary[i].Item1, boundary[i].Item2 - 1, 0] == 0 &&
                            inputimage.Data[boundary[i].Item1, boundary[i].Item2 - 1, 1] == 0 &&
                            inputimage.Data[boundary[i].Item1, boundary[i].Item2 - 1, 2] == 0))
                    {

                            normalvec[0] = -1;
                        normalvec[1] = 0;
                        double D = Math.Abs(1.0 * (grad[0] * normalvec[0] + grad[1] * normalvec[1])) / 255.0;


                        double C = 0;

                        for (int j = boundary[i].Item1 - blocksize / 2; j <= boundary[i].Item1 + blocksize / 2; j++)
                        {
                            for (int k = boundary[i].Item2 - blocksize / 2; k <= boundary[i].Item2 + blocksize / 2; k++)
                            {
                                if (j < 0 || j >= row || k < 0 || k >= col)
                                {
                                    continue;
                                }
                                if (inputimage.Data[j, k, 0] > 0 || inputimage.Data[j, k, 1] > 0 || inputimage.Data[j, k, 2] > 0)
                                {

                                    C += confidence[j, k];
                                }

                            }

                        }

                        C /= 1.0 * (blocksize * blocksize);

                        priority.Add(new Tuple<double, int, int>(C * D, boundary[i].Item1, boundary[i].Item2));

                    }

                    if (boundary[i].Item2 + 1 < col&& (inputimage.Data[boundary[i].Item1, boundary[i].Item2 + 1, 0] == 0 &&
                            inputimage.Data[boundary[i].Item1, boundary[i].Item2 + 1, 1] == 0 &&
                            inputimage.Data[boundary[i].Item1, boundary[i].Item2 + 1, 2] == 0))
                    {

                            normalvec[0] = 1;
                        normalvec[1] = 0;
                        double D = Math.Abs(1.0 * (grad[0] * normalvec[0] + grad[1] * normalvec[1])) / 255.0;


                        double C = 0;

                        for (int j = boundary[i].Item1 - blocksize / 2; j <= boundary[i].Item1 + blocksize / 2; j++)
                        {
                            for (int k = boundary[i].Item2 - blocksize / 2; k <= boundary[i].Item2 + blocksize / 2; k++)
                            {
                                if (j < 0 || j >= row || k < 0 || k >= col)
                                {
                                    continue;
                                }
                                if (inputimage.Data[j, k, 0] > 0 || inputimage.Data[j, k, 1] > 0 || inputimage.Data[j, k, 2] > 0)
                                {

                                    C += confidence[j, k];
                                }

                            }

                        }

                        C /= 1.0 * (blocksize * blocksize);

                        priority.Add(new Tuple<double, int, int>(C * D, boundary[i].Item1, boundary[i].Item2));
                    }

                    if (boundary[i].Item1 - 1 >= 0&&boundary[i].Item2-1>=0 && inputimage.Data[boundary[i].Item1 - 1, boundary[i].Item2-1, 0] == 0 &&
        inputimage.Data[boundary[i].Item1 - 1, boundary[i].Item2-1, 1] == 0 &&
        inputimage.Data[boundary[i].Item1 - 1, boundary[i].Item2-1, 2] == 0)
                    {
                        normalvec[1] = -1;
                        normalvec[0] = -1;
                        double D = Math.Abs(1.0 * (grad[0] * normalvec[0] + grad[1] * normalvec[1])) / 255.0;


                        double C = 0;

                        for (int j = boundary[i].Item1 - blocksize / 2; j <= boundary[i].Item1 + blocksize / 2; j++)
                        {
                            for (int k = boundary[i].Item2 - blocksize / 2; k <= boundary[i].Item2 + blocksize / 2; k++)
                            {
                                if (j < 0 || j >= row || k < 0 || k >= col)
                                {
                                    continue;
                                }
                                if (inputimage.Data[j, k, 0] > 0 || inputimage.Data[j, k, 1] > 0 || inputimage.Data[j, k, 2] > 0)
                                {

                                    C += confidence[j, k];
                                }

                            }

                        }

                        C /= 1.0 * (blocksize * blocksize);

                        priority.Add(new Tuple<double, int, int>(C * D, boundary[i].Item1, boundary[i].Item2));
                        normalvec[1] = 1;
                        normalvec[0] = 1;
                        D = Math.Abs(1.0 * (grad[0] * normalvec[0] + grad[1] * normalvec[1])) / 255.0;
                        priority.Add(new Tuple<double, int, int>(C * D, boundary[i].Item1, boundary[i].Item2));

                    }

                    if (boundary[i].Item1 - 1 >= 0&&boundary[i].Item2+1<col && inputimage.Data[boundary[i].Item1 - 1, boundary[i].Item2+1, 0] == 0 &&
        inputimage.Data[boundary[i].Item1 - 1, boundary[i].Item2+1, 1] == 0 &&
        inputimage.Data[boundary[i].Item1 - 1, boundary[i].Item2+1, 2] == 0)
                    {
                        normalvec[1] = -1;
                        normalvec[0] = 1;
                        double D = Math.Abs(1.0 * (grad[0] * normalvec[0] + grad[1] * normalvec[1])) / 255.0;


                        double C = 0;

                        for (int j = boundary[i].Item1 - blocksize / 2; j <= boundary[i].Item1 + blocksize / 2; j++)
                        {
                            for (int k = boundary[i].Item2 - blocksize / 2; k <= boundary[i].Item2 + blocksize / 2; k++)
                            {
                                if (j < 0 || j >= row || k < 0 || k >= col)
                                {
                                    continue;
                                }
                                if (inputimage.Data[j, k, 0] > 0 || inputimage.Data[j, k, 1] > 0 || inputimage.Data[j, k, 2] > 0)
                                {

                                    C += confidence[j, k];
                                }

                            }

                        }

                        C /= 1.0 * (blocksize * blocksize);

                        priority.Add(new Tuple<double, int, int>(C * D, boundary[i].Item1, boundary[i].Item2));
                        normalvec[1] = +1;
                        normalvec[0] = -1;
                        D = Math.Abs(1.0 * (grad[0] * normalvec[0] + grad[1] * normalvec[1])) / 255.0;
                        priority.Add(new Tuple<double, int, int>(C * D, boundary[i].Item1, boundary[i].Item2));
                    }

                    if (boundary[i].Item1 + 1 < row&&boundary[i].Item2-1>=0 && inputimage.Data[boundary[i].Item1 - 1, boundary[i].Item2-1, 0] == 0 &&
        inputimage.Data[boundary[i].Item1 + 1, boundary[i].Item2-1, 1] == 0 &&
        inputimage.Data[boundary[i].Item1 + 1, boundary[i].Item2-1, 2] == 0)
                    {
                        normalvec[1] = +1;
                        normalvec[0] = -1;
                        double D = Math.Abs(1.0 * (grad[0] * normalvec[0] + grad[1] * normalvec[1])) / 255.0;


                        double C = 0;

                        for (int j = boundary[i].Item1 - blocksize / 2; j <= boundary[i].Item1 + blocksize / 2; j++)
                        {
                            for (int k = boundary[i].Item2 - blocksize / 2; k <= boundary[i].Item2 + blocksize / 2; k++)
                            {
                                if (j < 0 || j >= row || k < 0 || k >= col)
                                {
                                    continue;
                                }
                                if (inputimage.Data[j, k, 0] > 0 || inputimage.Data[j, k, 1] > 0 || inputimage.Data[j, k, 2] > 0)
                                {

                                    C += confidence[j, k];
                                }

                            }

                        }

                        C /= 1.0 * (blocksize * blocksize);

                        priority.Add(new Tuple<double, int, int>(C * D, boundary[i].Item1, boundary[i].Item2));
                        normalvec[1] = -1;
                        normalvec[0] = 1;
                        D = Math.Abs(1.0 * (grad[0] * normalvec[0] + grad[1] * normalvec[1])) / 255.0;
                        priority.Add(new Tuple<double, int, int>(C * D, boundary[i].Item1, boundary[i].Item2));
                    }

                    if (boundary[i].Item1 + 1 < row &&boundary[i].Item2+1<col&& inputimage.Data[boundary[i].Item1 + 1, boundary[i].Item2+1, 0] == 0 &&
        inputimage.Data[boundary[i].Item1 + 1, boundary[i].Item2+1, 1] == 0 &&
        inputimage.Data[boundary[i].Item1 + 1, boundary[i].Item2+1, 2] == 0)
                    {
                        normalvec[1] = 1;
                        normalvec[0] =1;
                        double D = Math.Abs(1.0 * (grad[0] * normalvec[0] + grad[1] * normalvec[1])) / 255.0;


                        double C = 0;

                        for (int j = boundary[i].Item1 - blocksize / 2; j <= boundary[i].Item1 + blocksize / 2; j++)
                        {
                            for (int k = boundary[i].Item2 - blocksize / 2; k <= boundary[i].Item2 + blocksize / 2; k++)
                            {
                                if (j < 0 || j >= row || k < 0 || k >= col)
                                {
                                    continue;
                                }
                                if (inputimage.Data[j, k, 0] > 0 || inputimage.Data[j, k, 1] > 0 || inputimage.Data[j, k, 2] > 0)
                                {

                                    C += confidence[j, k];
                                }

                            }

                        }

                        C /= 1.0 * (blocksize * blocksize);

                        priority.Add(new Tuple<double, int, int>(C * D, boundary[i].Item1, boundary[i].Item2));
                        normalvec[1] = -1;
                        normalvec[0] = -1;
                        D = Math.Abs(1.0 * (grad[0] * normalvec[0] + grad[1] * normalvec[1])) / 255.0;
                        priority.Add(new Tuple<double, int, int>(C * D, boundary[i].Item1, boundary[i].Item2));
                    }




                }

                priority.Sort();
                int n = (priority).Count;
                int nextblock=getsimilar(new Tuple<int, int>( priority[n-1].Item2,priority[n-1].Item3 ) ) ;
                draw(nextblock, new Tuple<int, int>(priority[n - 1].Item2, priority[n - 1].Item3),priority[n-1].Item1);
                pictureBox4.Image = inputimage.ToBitmap();
                CvInvoke.WaitKey();
            }


        }
        private void button3_Click(object sender, EventArgs e)
        {
             block = new List<List<Tuple<int, int, int> > > ();
           blocksize= int.Parse(textBox1.Text);

            for (int i = 0; i < row; i++) {
                for (int j = 0; j < col; j++) {

                    if (i + blocksize < row && j + blocksize < col) {

                        List <  Tuple<int, int, int> > tmp = new List< Tuple<int, int, int> > ();
                        int flag = 0;
                        for (int t1 = i; t1 < i + blocksize&&flag==0; t1++) {
                            for (int t2 = j; t2 < j + blocksize&&flag==0; t2++) {
                                if (inputimage.Data[t1, t2, 0] == 0 
                                    && inputimage.Data[t1, t2, 1] == 0 
                                    && inputimage.Data[t1, t2, 2] == 0) {
                                    flag = 1;

                                    break;
                                }
                                tmp.Add(new Tuple<int, int, int>(inputimage.Data[t1, t2, 0], 
                                    inputimage.Data[t1, t2, 1],
                                    inputimage.Data[t1, t2, 2]));
                            }
                        }

                        if (flag == 0) {
                            
                            block.Add(new List < Tuple<int, int, int> > (tmp) );
                        }
                        tmp.Clear();


                    }

                }
            }

            solve();
            pictureBox3.Image = inputimage.ToBitmap();

        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            rightx= e.X;
            righty= e.Y;

            inputimage = oriimage.Copy();

            for (int i = lefty; i <= righty; i++) {
                for (int j = leftx; j <= rightx; j++) {
                    inputimage.Data[i, j,0] = 0;
                    inputimage.Data[i, j, 1] = 0;
                    inputimage.Data[i, j, 2] = 0;
                    confidence[i, j] = 0;
                }
            }
            pictureBox2.Image = inputimage.ToBitmap();

        }

        private void button1_Click(object sender, EventArgs e)
        {
           
            OpenFileDialog Openfile = new OpenFileDialog();
            Openfile.ShowDialog();

            oriimage = new Image<Bgr, byte>(Openfile.FileName);

            pictureBox1.Image = oriimage.ToBitmap();
            col = oriimage.Width;
            row = oriimage.Height;
            for (int i = 0; i < row; i++) {
                for (int j = 0; j < col; j++) {
                    if(oriimage.Data[i,j,0]==0)
                        oriimage.Data[i, j, 0] = 1;

                    confidence[i, j] = 1;
                }
            }

        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            leftx = e.X;
           lefty = e.Y;

        }
    }
}
