using AutoMapper;

using Demo.Application.Mapping;

namespace Demo.Application.Tests.TestHelpers;

/// <summary>
/// Tạo mapper thật từ profile ứng dụng để test service không phụ thuộc cấu hình giả.
/// </summary>
internal static class TestMapperFactory
{
    public static IMapper Create()
    {
        var configuration = new MapperConfiguration(config => config.AddProfile<BackendDataAccessProfile>());
        configuration.AssertConfigurationIsValid();
        return configuration.CreateMapper();
    }
}
