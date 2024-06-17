using System;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace See.Sed.FichaAluno.Compatibiarra.Util
{
    public class Mail
    {
        public static bool EnviarEmailBodyHtmlGmail(string To, string cc, string Subject, string Message)
        {
            var email = new MailMessage();
            try
            {
                if (!string.IsNullOrEmpty(To))
                {
                    EnviarEmail(To, Subject, Message);
                }
                if (!string.IsNullOrEmpty(cc))
                {
                    var emails = cc.Split(',');
                    for (int i = 0; i < emails.Count(); i++)
                    {
                        if (!string.IsNullOrEmpty(emails[i]))
                        {
                            EnviarEmail(emails[i], Subject, Message);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
                Program.Msg(ex.Message + ex.StackTrace);
                return false;
            }
            finally
            {
                email.Dispose();
            }
        }

        public static bool EnviarEmailBodyHtml2(Prodesp.DataAccess.IDataBase db, string To, string cc, string Subject, string Message)
        {
            var email = new MailMessage();
            string Retorno = string.Empty;
            try
            {
                string numeroServer = string.Empty;

                // faz 3 tentativas para pegar o servidor
                for (int i = 0; i < 3; i++)
                {
                    numeroServer = GetNumberServer(db);
                    if (!string.IsNullOrEmpty(numeroServer)) break;
                }

                if (string.IsNullOrEmpty(numeroServer)) return false;

                using (SmtpClient smtp = GetSmtp(numeroServer))
                {
                    email.To.Add(new MailAddress(To));
                    if (!string.IsNullOrEmpty(cc))
                    {
                        var emails = cc.Split(',');
                        for (int i = 0; i < emails.Count(); i++)
                        {
                            if (!string.IsNullOrEmpty(emails[i]))
                                email.CC.Add(new MailAddress(emails[i]));
                        }

                    }
                    //email.To.Add(new MailAddress("sedteste@gmail.com"));
                    email.From = new MailAddress("no-reply.sed" + numeroServer.PadLeft(2, '0') + "email", "email");
                    email.Body = Message;
                    email.IsBodyHtml = true;
                    email.Subject = Subject.Trim();
                    email.BodyEncoding = Encoding.UTF8;

                    smtp.Send(email);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
                Program.Msg(ex.Message + ex.StackTrace);
                return false;
            }
            finally
            {
                email.Dispose();
                email = null;
            }
        }

        private static SmtpClient GetSmtp(string numeroServer)
        {
            SmtpClient smtp = new SmtpClient();
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new System.Net.NetworkCredential("no-reply.sed" + numeroServer.PadLeft(2, '0') + "email", "**" + numeroServer.PadLeft(3, '0'));
            smtp.Port = 587;
            smtp.Host = "smtp.office365.com";
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.EnableSsl = true;
            return smtp;
        }

        private static string GetNumberServer(Prodesp.DataAccess.IDataBase db)
        {
            db.ClearParameters();
            db.AddParameter("SENDER", 0, ParameterDirection.Output);
            db.ExecuteNonQuery(CommandType.StoredProcedure, "[DESENV].[dbo].[SP_GET_NEXT_SENDER_EMAIL_SED]");
            return db.GetParameter("SENDER").Value.ToString();
        }
        public static bool EnviarEmail(string To, string Subject, string Message)
        {
            MailMessage email = new MailMessage();
            try
            {
                if (!string.IsNullOrEmpty(To))
                {
                    SmtpClient smtp = new SmtpClient();
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new System.Net.NetworkCredential("email", "**");
                    smtp.Port = 587;
                    smtp.Host = "smtp.gmail.com";
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.EnableSsl = true;
                    email.To.Add(new MailAddress(To));
                    email.From = new MailAddress("email", "email");
                    email.Body = Message;
                    email.IsBodyHtml = true;
                    email.Subject = Subject.Trim();
                    email.BodyEncoding = Encoding.UTF8;
                    smtp.Send(email);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
                Program.Msg(ex.Message + ex.StackTrace);
                return false;
            }
            finally
            {
                email.Dispose();
            }
        }
    }
}