using AutoMapper;

namespace Notion.Sync.Api.Business
{
    public class BaseService<T> where T : class
    {
        protected readonly IMapper _mapper;
        protected readonly ILogger<T> _logger;
        protected BaseService(IMapper mapper, ILogger<T> logger)
        {
            _mapper = mapper;
            _logger = logger;
        }
    }
}
