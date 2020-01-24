using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SmtpMoq.NET.Test.E2E
{
    public class SmtpServerConcurentTest : SmtpServerBaseTest
    {
        public SmtpServerConcurentTest(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Theory, MemberData(nameof(AvailableSmtpSenders))]
        public void TestSendingConcurentEmails(SmtpSenderDelegate smtpSender)
        {
            int tasksCount = 9;

            var senderTasks = new List<Task>();
            for (int i = 1; i <= tasksCount; i++)
            {
                senderTasks.Add(Task.Run(
                    () => smtpSender(testFromAddress, testToAddress, testSubject + i, testBody + i)));
            }

            Task.WaitAll(senderTasks.ToArray());

            Assert.Equal(tasksCount, this.server.ReceivedMessages.MessageCount);
        }
    }
}
