using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace qed
{
    public enum LineEnding
    {
        Unknown,
        DOS,
        Unix,
        Mixed
    }

    class LineCollection
    {
        private List<Line> lineList;
        int longestLineIndex;
        int longestLineLength;
        bool knowLongestLine;

        bool suspendEvents = false;
        public event EventHandler LineCountChanged;

        LineEnding lineEnding = LineEnding.Unknown;
        Encoding encoding = Encoding.Default;

        public LineCollection()
        {
            lineList = new List<Line>();
            Clear();
        }

        public LineEnding LineEnding
        {
            get
            {
                return lineEnding;
            }
        }

        public Encoding Encoding
        {
            get
            {
                return encoding;
            }
        }

        public void Open(string filename)
        {
            int nCR = 0;
            int nLF = 0;
            int nCRLF = 0;

            SuspendEvents();

            try
            {
                using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    Clear();
                    StringBuilder sb = new StringBuilder();
                    StreamReader sr = new StreamReader(stream, Encoding.Default, true);
                    int ch = sr.Read();
                    while (ch != -1)
                    {
                        if (ch == '\r' || ch == '\n')
                        {
                            if (ch == '\r')
                            {
                                ch = sr.Read();
                                if (ch == '\n')
                                {
                                    ch = sr.Read();
                                    ++nCRLF;
                                }
                                else
                                    ++nCR;

                            }
                            else if (ch == '\n')
                            {
                                ch = sr.Read();
                                ++nLF;
                            }
                            else
                                ch = sr.Read();
                            Line line = new Line(sb.ToString());
                            lineList.Add(line);
                            sb.Length = 0;
                        }
                        else
                        {
                            sb.Append((char)ch);
                            ch = sr.Read();
                        }
                    }

                    if (sb.Length > 0)
                        lineList.Add(new Line(sb.ToString()));

                    encoding = sr.CurrentEncoding;
                }

                if (nCRLF > 0 && nCR == 0 && nLF == 0)
                    lineEnding = LineEnding.DOS;
                else if (nCRLF == 0 && nLF > 0 && nCR == 0)
                    lineEnding = LineEnding.Unix;
                else
                    lineEnding = LineEnding.Mixed;
            }
            finally
            {
                ResumeEvents();
            }
        }

        public void Save(string file)
        {
            using (StreamWriter sw = new StreamWriter(file, false, Encoding))
            {
                for (int i = 0; i < lineList.Count; ++i)
                {
                    sw.Write(lineList[i].Text);
                    if (LineEnding == LineEnding.Unix)
                        sw.Write("\n");
                    else
                        sw.Write("\r\n");
                }
            }
        }

        public Line this[int index]
        {
            get
            {
                return lineList[index];
            }
            set
            {
                if (index == longestLineIndex)
                {
                    if (value.Text.Length > longestLineLength)
                    {
                        longestLineLength = value.Text.Length;
                    }
                    else
                        knowLongestLine = false;
                }
                else
                {
                    CheckForLongestLine(index, value);
                }
                lineList[index] = value;
            }
        }

        private void EnsureLongestLine()
        {
            if (!knowLongestLine)
            {
                longestLineLength = -1;
                longestLineIndex = -1;
                for (int i = 0; i < lineList.Count; ++i)
                {
                    CheckForLongestLine(i, lineList[i]);
                }
                knowLongestLine = true;
            }
        }

        public int LongestLineLength
        {
            get
            {
                EnsureLongestLine();
                return longestLineLength >= 0 ? longestLineLength : 0;
            }
        }

        public int LongestLineIndex
        {
            get
            {
                EnsureLongestLine();
                return longestLineIndex >= 0 ? longestLineIndex : 0;
            }
        }

        public int Count
        {
            get
            {
                return lineList.Count;
            }
        }

        private void CheckForLongestLine(int index, Line line)
        {
            if (line.Text.Length > longestLineLength)
            {
                longestLineIndex = index;
                longestLineLength = line.Text.Length;
                knowLongestLine = true;
            }
        }

        public void Insert(int index, Line line)
        {
            lineList.Insert(index, line);
            FireLineCountChanged();
            CheckForLongestLine(index, line);
        }

        public void Add(Line line)
        {
            lineList.Add(line);
            FireLineCountChanged();
            CheckForLongestLine(lineList.Count - 1, line);
        }

        public void RemoveAt(int index)
        {
            lineList.RemoveAt(index);
            FireLineCountChanged();
            if (index == longestLineIndex)
                knowLongestLine = false;
        }

        public void RemoveRange(int index, int count)
        {
            lineList.RemoveRange(index, count);
            FireLineCountChanged();
            if (index <= longestLineIndex && longestLineIndex < index + count)
                knowLongestLine = false;
        }

        public void Clear()
        {
            lineList.Clear();
            longestLineIndex = -1;
            longestLineLength = -1;
            knowLongestLine = false;
            FireLineCountChanged();
        }

        public void SuspendEvents()
        {
            suspendEvents = true;
        }

        public void ResumeEvents()
        {
            suspendEvents = false;
            FireLineCountChanged();
        }

        private void FireLineCountChanged()
        {
            if (LineCountChanged != null && !suspendEvents)
                LineCountChanged.Invoke(this, EventArgs.Empty);
        }
    }
}
