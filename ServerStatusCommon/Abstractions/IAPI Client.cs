// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Models.Requests.Create;
using ServerStatusCommon.Models.Requests.Update;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Responses.Related;

namespace ServerStatusCommon.Abstractions
{
    /// <summary>
    /// Interface for the API.
    /// </summary>
    public interface IAPIClient
    {
        void SetBearerToken(string bearerToken);
        Task<(AuthenticationModel?, ResponseModel?)> Authorise();
        Task<(PagedResponseModel<UserModel>?, bool)> GetUsers(List<KeyValuePair<string, object>> queryParameters);
        Task<(List<UserSettingModel>, bool)> GetUserSettings(int userId);
        Task<(PagedResponseModel<ServerModel>?, bool)> GetServers(List<KeyValuePair<string, object>> queryParameters);
        Task<(List<EventModel>, bool)> GetServerEvents(List<KeyValuePair<string, object>> queryParameters);
        Task<(SettingModel?, ResponseModel?)> UpdateUserSettings(int userSettingId, UserSettingUpdateRequestModel userSetting);
        Task<(UserModel?, ResponseModel?)> UpdateUser(int userId, UserUpdateRequestModel user);
        Task<(PagedResponseModel<AlertModel>?, bool)> GetAlerts(List<KeyValuePair<string, object>> queryParameters);
        Task<(AlertModel?, bool)> GetAlert(int alertId);
        Task<(AlertModel?, ResponseModel?)> UpdateAlert(int alertId, AlertUpdateRequestModel alert);
        Task<(AlertModel?, ResponseModel?)> RegisterAlert(AlertRequestModel alert);
        Task<(EventModel?, ResponseModel?)> RegisterServerEvent(EventRequestModel newEvent);
        Task<(List<ComponentModel>, bool)> GetComponents();
    }
}
