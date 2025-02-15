﻿using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text;
using System.Text.RegularExpressions;
using MissionPlanner.Controls;
using System.Threading;

namespace MissionPlanner.MsgBox
{
    public static class CustomMessageBox
    {
        const int FORM_Y_MARGIN = 10;
        const int FORM_X_MARGIN = 16;

       public delegate void ThemeManager(Control ctl);

        public static event ThemeManager ApplyTheme;

        static DialogResult _state = DialogResult.None;

        public static DialogResult Show(string text)
        {
            return Show(text, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        public static DialogResult Show(string text, string caption)
        {
            return Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
        {
            return Show(text, caption, buttons, MessageBoxIcon.None);
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, string YesText = "Sim", string NoText = "Não") // traduzido
        {
            DialogResult answer = DialogResult.Cancel;

            Console.WriteLine("CustomMessageBox thread calling " + System.Threading.Thread.CurrentThread.Name);

            // ensure we run this on the right thread - mono - mac
            if (Application.OpenForms.Count > 0 && Application.OpenForms[0].InvokeRequired)
            {
                try
                {
                    Application.OpenForms[0].Invoke((Action) delegate
                    {
                        Console.WriteLine("CustomMessageBox thread running invoke " +
                                          System.Threading.Thread.CurrentThread.Name);
                        answer = ShowUI(text, caption, buttons, icon, YesText, NoText);
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    // fall back
                    Console.WriteLine("CustomMessageBox thread running " + System.Threading.Thread.CurrentThread.Name);
                    answer = ShowUI(text, caption, buttons, icon, YesText, NoText);
                }
            }
            else
            {
                Console.WriteLine("CustomMessageBox thread running " + System.Threading.Thread.CurrentThread.Name);
                answer =  ShowUI(text, caption, buttons, icon, YesText, NoText);
            }

            return answer;
        }

        static DialogResult ShowUI(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, string YesText = "Sim", string NoText = "Não")//traduzido
        {
            DialogResult answer = DialogResult.Abort;

            if (text == null)
                text = "";

            if (caption == null)
                caption = "";

            string link = "";
            string linktext = "";


            Regex linkregex = new Regex(@"(\[link;([^\]]+);([^\]]+)\])", RegexOptions.IgnoreCase);
            Match match = linkregex.Match(text);
            if (match.Success)
            {
                link = match.Groups[2].Value;
                linktext = match.Groups[3].Value;
                text = text.Replace(match.Groups[1].Value, "");
            }

            // ensure we are always in a known state
            _state = DialogResult.None;
            
            SizeF sz = TextRenderer.MeasureText ("The quick brown Fox", SystemFonts.DefaultFont);
            var perchar = sz.Width / 20;
            // convert to nice wrapped lines.
            text = AddNewLinesToText(text, Screen.PrimaryScreen.Bounds.Width / (int)perchar);
            // get pixel width and height
            Size textSize = TextRenderer.MeasureText(text, SystemFonts.DefaultFont);
            // allow for icon
            if (icon != MessageBoxIcon.None)
                textSize.Width += SystemIcons.Question.Width;

            using (var msgBoxFrm = new Form
            {
                FormBorderStyle = FormBorderStyle.FixedDialog,
                ShowInTaskbar = true,
                StartPosition = FormStartPosition.CenterParent,
                Text = caption,
                MaximizeBox = false,
                MinimizeBox = false,
                Width = textSize.Width + 20,
                Height = textSize.Height + 120,
                TopMost = true,
                AutoScaleMode = AutoScaleMode.None,
            })
            {

                Rectangle screenRectangle = msgBoxFrm.RectangleToScreen(msgBoxFrm.ClientRectangle);
                int titleHeight = screenRectangle.Top - msgBoxFrm.Top;

                var lblMessage = new Label
                {
                    Left = 58,
                    Top = 15,
                    Width = textSize.Width + 10,
                    Height = textSize.Height + 10,
                    Text = text,
                    AutoSize = true
                };

                msgBoxFrm.Controls.Add(lblMessage);

                msgBoxFrm.Width = lblMessage.Right + 50;

                if (link != "" && linktext != "")
                {
                    linktext = AddNewLinesToText(linktext);
                    Size textSize2 = TextRenderer.MeasureText(linktext, SystemFonts.DefaultFont);
                    var linklbl = new LinkLabel
                    {
                        Left = FORM_X_MARGIN,
                        Top = lblMessage.Bottom,
                        Width = textSize2.Width,
                        Height = textSize2.Height,
                        Text = linktext,
                        Tag = link,
                        AutoSize = true
                    };
                    linklbl.Click += (sender, args) =>
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(((LinkLabel)sender).Tag.ToString());
                        }
                        catch (Exception)
                        {
                            Show("Failed to open link " + ((LinkLabel)sender).Tag.ToString());
                        }
                    };

                    msgBoxFrm.Controls.Add(linklbl);

                    msgBoxFrm.Width = Math.Max(msgBoxFrm.Width, linklbl.Right + 16);
                }

                var actualIcon = getMessageBoxIcon(icon);

                if (actualIcon == null)
                {
                    lblMessage.Location = new Point(FORM_X_MARGIN, FORM_Y_MARGIN);
                }
                else
                {
                    var iconPbox = new PictureBox
                    {
                        Image = actualIcon.ToBitmap(),
                        Location = new Point(FORM_X_MARGIN, FORM_Y_MARGIN)
                    };
                    msgBoxFrm.Controls.Add(iconPbox);
                }


                AddButtonsToForm(msgBoxFrm, buttons, YesText, NoText);

                // display even if theme fails
                try
                {
                    if (ApplyTheme != null)
                        ApplyTheme(msgBoxFrm);
                    //ThemeManager.ApplyThemeTo(msgBoxFrm);
                }
                catch
                {
                }

                DialogResult test = msgBoxFrm.ShowDialog();

                answer = _state;
            }

            return answer;
        }

        // from http://stackoverflow.com/questions/2512781/winforms-big-paragraph-tooltip/2512895#2512895

        private static string AddNewLinesToText(string text, int length = 85)
        {
            if (text.Length < length)
                return text;
            int lineLength = length;
            StringBuilder sb = new StringBuilder();
            int currentLinePosition = 0;
            for (int textIndex = 0; textIndex < text.Length; textIndex++)
            {
                // If we have reached the target line length and the next      
                // character is whitespace then begin a new line.   
                if (currentLinePosition >= lineLength &&
                    char.IsWhiteSpace(text[textIndex]))
                {
                    sb.Append(Environment.NewLine);
                    currentLinePosition = 0;
                }
                // reset line lnegth counter on existing new line
                if (text[textIndex] == Environment.NewLine[Environment.NewLine.Length -1])
                {
                    currentLinePosition = 1;
                }
                // If we have just started a new line, skip all the whitespace.    
                if (currentLinePosition == 0)
                    while (textIndex < text.Length && char.IsWhiteSpace(text[textIndex]))
                        textIndex++;
                // Append the next character.     
                if (textIndex < text.Length) sb.Append(text[textIndex]);
                currentLinePosition++;
            }
            return sb.ToString();
        }

        private static void AddButtonsToForm(Form msgBoxFrm, MessageBoxButtons buttons, string YesText = "Sim", string NoText = "Não")//traduzido
        {
            Rectangle screenRectangle = msgBoxFrm.RectangleToScreen(msgBoxFrm.ClientRectangle);
            int titleHeight = screenRectangle.Top - msgBoxFrm.Top;

            var t = Type.GetType("Mono.Runtime");
            if ((t != null))
                titleHeight = 25;

            switch (buttons)
            {
                case MessageBoxButtons.OK:
                    var but = new MyButton
                                  {
                                      Size = new Size(75, 23),
                                      Text = "OK",
                                      Left = msgBoxFrm.Width - 100 - FORM_X_MARGIN,
                                      Top = msgBoxFrm.Height - 40 - FORM_Y_MARGIN - titleHeight
                                  };

                    but.Click += delegate { _state = DialogResult.OK; msgBoxFrm.Close(); };
                    msgBoxFrm.Controls.Add(but);
                    msgBoxFrm.AcceptButton = but;
                    break;

                case MessageBoxButtons.YesNo:

                    if (msgBoxFrm.Width < (75 * 2 + FORM_X_MARGIN * 3))
                        msgBoxFrm.Width = (75 * 2 + FORM_X_MARGIN * 3);

                    var butyes = new MyButton
                    {
                        Size = new Size(75, 23),
                        Text = YesText,
                        Left = msgBoxFrm.Width - 75 * 2 - FORM_X_MARGIN * 2,
                        Top = msgBoxFrm.Height - 23 - FORM_Y_MARGIN - titleHeight
                    };

                    butyes.Click += delegate { _state = DialogResult.Yes; msgBoxFrm.Close(); };
                    msgBoxFrm.Controls.Add(butyes);
                    msgBoxFrm.AcceptButton = butyes;

                    var butno = new MyButton
                    {
                        Size = new Size(75, 23),
                        Text = NoText,
                        Left = msgBoxFrm.Width - 75 - FORM_X_MARGIN,
                        Top = msgBoxFrm.Height - 23 - FORM_Y_MARGIN - titleHeight
                    };

                    butno.Click += delegate { _state = DialogResult.No; msgBoxFrm.Close(); };
                    msgBoxFrm.Controls.Add(butno);
                    msgBoxFrm.CancelButton = butno;
                    break;

                case MessageBoxButtons.OKCancel:

                    if (msgBoxFrm.Width < (75 * 2 + FORM_X_MARGIN * 3))
                        msgBoxFrm.Width = (75 * 2 + FORM_X_MARGIN * 3);

                    var butok = new MyButton
                    {
                        Size = new Size(75, 23),
                        Text = "OK",
                        Left = msgBoxFrm.Width - 75 * 2 - FORM_X_MARGIN * 2,
                        Top = msgBoxFrm.Height - 23 - FORM_Y_MARGIN - titleHeight
                    };

                    butok.Click += delegate { _state = DialogResult.OK; msgBoxFrm.Close(); };
                    msgBoxFrm.Controls.Add(butok);
                    msgBoxFrm.AcceptButton = butok;

                    var butcancel = new MyButton
                    {
                        Size = new Size(75, 23),
                        Text = "Cancel",
                        Left = msgBoxFrm.Width - 75 - FORM_X_MARGIN,
                        Top = msgBoxFrm.Height - 23 - FORM_Y_MARGIN - titleHeight
                    };

                    butcancel.Click += delegate { _state = DialogResult.Cancel; msgBoxFrm.Close(); };
                    msgBoxFrm.Controls.Add(butcancel);
                    msgBoxFrm.CancelButton = butcancel;
                    break;

                default:
                    throw new NotImplementedException("Only MessageBoxButtons.OK and YesNo supported at this time");
            }
        }

        /// <summary>
        /// Get system icon for MessageBoxIcon.
        /// </summary>
        /// <param name="icon">The MessageBoxIcon value.</param>
        /// <returns>SystemIcon type Icon.</returns>
        private static Icon getMessageBoxIcon(MessageBoxIcon icon)
        {
            switch (icon)
            {
                case MessageBoxIcon.Asterisk:
                    return SystemIcons.Asterisk;
                case MessageBoxIcon.Error:
                    return SystemIcons.Error;
                case MessageBoxIcon.Exclamation:
                    return SystemIcons.Exclamation;
                case MessageBoxIcon.Question:
                    return SystemIcons.Question;
                default:
                    return null;
            }
        }

    }
}