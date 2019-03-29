using System;

namespace SabberStoneCoreAi.Utils
{
	class ProgressBar
	{
        int total;
        int steps;
        int cursorPos;
        bool endline;

        public ProgressBar(int total, int steps = 30, bool endline = true){
            this.total = total;
            this.steps = steps;
            this.endline = endline;
        }

        public void Update(int current){
            cursorPos = 0;
            Console.CursorLeft = cursorPos;
            Console.Write("["); //start
            Console.CursorLeft = cursorPos+steps+1;
            Console.Write("]"); //end
            Console.CursorLeft = cursorPos+1;

            for (int i = 0; i < steps; i++){
                if (current>=i*total/steps)
                    Console.Write("-");
                else break;
            }

            if (endline)
                Console.WriteLine();
        }


    }
}

