using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/*
 * FILE             : Program.cs
 * PROJECT          : PROG2121: Assignment #4
 * PROGRAMMER       : Devin Graham
 * FIRST VERSION    : 2021-10-18
 * DESCRIPTION      : 
 *      The purpose of this program is to retrieve two command line arguments from the user.
 *      The first argument is a complete file path to a file and the second is the max number
 *      of bytes that will be written to the file. The program creates 50 identical threads that
 *      generates a 50 character string and writes it to a file. When the file reaches the value
 *      specified by the second argument the threads are ended in a controlled manner and the final
 *      byte count is displayed.
 */

namespace PROG2121_Threads
{
    
    class Program
    {

        public static volatile bool done = false;
        private static Mutex mut = new Mutex();

        static void Main(string[] args)
        {
            //Display usage message for incorrect number of command line arguments
            if(args.Length == 0 || args.Length >= 3)
            {
                Console.WriteLine("usage: Incorrect number of arguments use /? for help");
                Console.ReadKey();
                return;
            }

            //Display usage message to help user understand the arguments
            if(args[0] == "/?")
            {
                Console.WriteLine("usage: <full pathname> <size of file to be created>");
                Console.ReadKey();
                return;
            }

            string path = args[0];
            int size;
            string filename = null;
            string directory = null;

            //Check for valid file path
            if (Path.IsPathRooted(path))
            {
                //Get the filename and directory from the path
                try
                {
                    filename = Path.GetFileName(path);
                    directory = Path.GetDirectoryName(path);
                }
                catch(Exception Ex)
                {
                    Console.WriteLine(Ex.ToString());
                }

                //Check if valid file was inputted
                if (filename == null || filename == "")
                {
                    Console.WriteLine("usage: Missing filename");
                    Console.ReadKey();
                    return;
                }

                //Check if the directory entered exists
                if(!Directory.Exists(directory))
                {
                    //Create directory from the first argument
                    try
                    {
                        Directory.CreateDirectory(directory);
                        var myFile = File.Create(path);
                        myFile.Close();
                    }
                    catch (Exception Ex)
                    {
                        Console.WriteLine(Ex.ToString());
                    }
                }
                else
                {
                    //Check if file exists and if it doesn't create it
                    if(!File.Exists(path))
                    {
                        var myFile = File.Create(path);
                        myFile.Close();
                    }
                }
                
            }
            else
            {
                Console.WriteLine("usage: arg 0 not a valid file path");
                Console.ReadKey();
                return;
            }

            bool checkSize = int.TryParse(args[1], out size);

            //Check if second argument is an integer
            if(checkSize)
            {
                //Check if second argument is in range
                if((size < 1000) || (size > 20000000))
                {
                    Console.WriteLine("usage: arg 1 out of range");
                    Console.ReadKey();
                    return;
                }
            }
            else
            {
                Console.WriteLine("usage: arg 1 not a valid integer");
                Console.ReadKey();
                return;
            }

            
            Thread[] threads = new Thread[50];
            Thread checkerThread = new Thread(checkThreads);
            object arguments = new object[2] {path, size};


            checkerThread.Start(arguments);
            
            //Create threads to generate strings and store them in the array
            for (int i = 0; i < 50; i++)
            {
                threads[i] = new Thread(writeRandomLine);
            }

            //Start generation threads
            for(int i = 0; i < 50; i++)
            {
                threads[i].Start(path);
            }

            //Join generation threads
            for (int i = 0; i < 50; i++)
            {
                threads[i].Join();
            }

            long length = new FileInfo(path).Length;
            
            Console.WriteLine("The final size of " + path + " is " + length + " bytes");
            Console.ReadKey();
        }



        /*
         * FUNCTION     : checkThreads
         * DESCRIPTION  :
         *      This function checks the size of a file for its 
         *      length in bytes. When the size is equal or greater
         *      than the length specified it calls the volatile bool
         *      to stop the threads from writing to the file
         * PARAMETERS   :
         *      object args : Contains the size of the file and its path
         * RETURNS      : 
         *      void
         */
        static void checkThreads(object args)
        {
            Array argArray = new object[2];
            argArray = (Array)args;

            string path = (string)argArray.GetValue(0);
            int size = (int)argArray.GetValue(1);

            //Check file until file length is reached
            while (done == false)
            {
                long length = new FileInfo(path).Length;

                Console.WriteLine("Size of " + path + " is " + length + " bytes");

                //Call volatile bool when file length is reached or exceeded
                if (length >= size)
                {
                    done = true;
                }
               
                Thread.Sleep(1000);
            }
        }



        /*
         * FUNCTION     : writeRandomLine
         * DESCRIPTION  :
         *      This function checks generates a random 50 character
         *      string and writes it to a specified file
         * PARAMETERS   :
         *      object args : Contains a path to a file to write to
         * RETURNS      : 
         *      void
         */
        static void writeRandomLine(object filepath)
        {
            char[] letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            Random r = new Random();
            string randomString = "";

            string filepathString = Convert.ToString(filepath);

            //Continue to write lines until the volatile bool is called
            while (done == false)
            {
                //Check if mutex can be aquired
                if (mut.WaitOne())
                {
                    //Write to the file, reset the string, and release mutex
                    try
                    {
                        using (StreamWriter sw = File.AppendText(filepathString))
                        {
                            //Generate random string
                            for (int j = 0; j < 50; j++)
                            {
                                randomString += letters[r.Next(0, 51)].ToString();
                            }
                            sw.WriteLine(randomString);
                            sw.Close();
                        }
                    }
                    catch(Exception Ex)
                    {
                        Console.WriteLine(Ex.ToString());
                    }
                    finally
                    {
                        randomString = "";
                        mut.ReleaseMutex();
                    }
                }
            }
            
        }
    }
}
