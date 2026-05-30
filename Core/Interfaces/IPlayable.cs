using System;
using System.Threading.Tasks;

namespace CaseForgeAI.Core.Interfaces
{
    public interface IPlayable
    {
        Guid Id { get; }
        string Title { get; }
        string Description { get; }
        bool IsActive { get; }
        Task StartPlayAsync(Guid userId);
        Task CompletePlayAsync(int score);
    }
}
