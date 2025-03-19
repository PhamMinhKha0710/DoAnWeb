using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using DoAnWeb.Services;
using System.Collections.Concurrent;

namespace DoAnWeb.Hubs
{
    /// <summary>
    /// SignalR Hub để xử lý việc đếm lượt xem theo thời gian thực
    /// </summary>
    public class ViewCountHub : Hub
    {
        private readonly IQuestionService _questionService;
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int, bool>> _userViewedQuestions = new();

        public ViewCountHub(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        /// <summary>
        /// Phương thức để tăng số lượt xem khi người dùng cuộn đến giữa trang
        /// </summary>
        /// <param name="questionId">ID của câu hỏi đang xem</param>
        /// <returns>Task</returns>
        public async Task IncreaseViewCount(int questionId)
        {
            string connectionId = Context.ConnectionId;

            // Kiểm tra xem người dùng đã xem câu hỏi này chưa trong phiên này
            if (!HasUserViewedQuestion(connectionId, questionId))
            {
                try
                {
                    // Cập nhật lượt xem và đánh dấu người dùng đã xem
                    int newViewCount = _questionService.UpdateViewCount(questionId);
                    MarkQuestionAsViewed(connectionId, questionId);

                    // Gửi cập nhật số lượt xem mới đến tất cả client
                    await Clients.All.SendAsync("ReceiveUpdatedViewCount", questionId, newViewCount);
                }
                catch (Exception ex)
                {
                    // Ghi log lỗi (có thể thêm logging service)
                    Console.WriteLine($"Error increasing view count: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Phương thức để lấy số lượt xem hiện tại của câu hỏi
        /// </summary>
        /// <param name="questionId">ID của câu hỏi</param>
        /// <returns>Task</returns>
        public async Task GetCurrentViewCount(int questionId)
        {
            try
            {
                int currentViewCount = _questionService.GetViewCount(questionId);
                await Clients.Caller.SendAsync("ReceiveCurrentViewCount", questionId, currentViewCount);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                Console.WriteLine($"Error getting view count: {ex.Message}");
            }
        }

        /// <summary>
        /// Kiểm tra xem người dùng đã xem câu hỏi này chưa trong phiên này
        /// </summary>
        private bool HasUserViewedQuestion(string connectionId, int questionId)
        {
            if (_userViewedQuestions.TryGetValue(connectionId, out var viewedQuestions))
            {
                return viewedQuestions.ContainsKey(questionId);
            }
            return false;
        }

        /// <summary>
        /// Đánh dấu câu hỏi đã được xem bởi người dùng
        /// </summary>
        private void MarkQuestionAsViewed(string connectionId, int questionId)
        {
            var viewedQuestions = _userViewedQuestions.GetOrAdd(connectionId, _ => new ConcurrentDictionary<int, bool>());
            viewedQuestions.TryAdd(questionId, true);
        }

        /// <summary>
        /// Xử lý khi người dùng ngắt kết nối
        /// </summary>
        public override Task OnDisconnectedAsync(Exception exception)
        {
            // Xóa dữ liệu người dùng khi họ ngắt kết nối
            _userViewedQuestions.TryRemove(Context.ConnectionId, out _);
            return base.OnDisconnectedAsync(exception);
        }
    }
} 