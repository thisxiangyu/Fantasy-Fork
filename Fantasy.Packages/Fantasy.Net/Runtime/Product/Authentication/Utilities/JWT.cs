#if FANTASY_NET
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.Helper;
using Microsoft.IdentityModel.Tokens;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.

namespace Fantasy.Product.Authentication
{
    /// <summary>
    /// JWT 配置类
    /// </summary>
    public class JwtConfig
    {
        /// <summary>
        /// 密钥
        /// </summary>
        public string SecretKey { get; set; } = "HighlyProtectedJwtSecretKey_ForTokenGeneration_7733AA55";
        /// <summary>
        /// 令牌发布者
        /// </summary>
        public string Issuer { get; set; } = "MyServerIssuer";
        /// <summary>
        /// 受众
        /// </summary>
        public string Audience { get; set; } = "MyClient";
    }

    /// <summary>
    /// 网络通行令牌中心, 用于读取配置、生成和验证通行令牌（Json Web Token, 通常简称JWT）。
    /// </summary>
    public partial class JWT : Singleton<JWT>,ITool,IAssemblyLifecycle
    {
        private static JwtConfig _config;
        private const string _JwtConfig = "JwtConfig.json";

        private readonly static JwtSecurityTokenHandler _tokenHandler = new JwtSecurityTokenHandler();
        private static SymmetricSecurityKey _key = default;       
        private static SigningCredentials _credentials = default;
        private static TokenValidationParameters _tokenValidationParameters = default;
        private static string _algorithms = SecurityAlgorithms.HmacSha256;

        private void UpdateConfig(JwtConfig config)
        {
            _config = config;
            _key = new(Encoding.UTF8.GetBytes(_config.SecretKey));
            _credentials = new SigningCredentials(_key, _algorithms);
            _tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _config.Issuer,

                ValidateAudience = true,
                ValidAudience = _config.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30) //允许时间误差
            };
        }

        /// <summary>
        /// 是否已启用
        /// </summary>
        public bool IsEnabled { get; private set; } = false;

        /// <summary>
        /// 启用
        /// </summary>
        public async FTask Enable()
        {
            if (!IsEnabled)
            { 
                await AssemblyLifecycle.Add(this);
                IsEnabled = true;
            }
        }

        private string _AssemblyFullName => _assemblyfullname ??= typeof(JWT).Assembly.GetName().FullName;
        private string? _assemblyfullname;

        /// <summary>
        /// 程序集加载时调用(启动和热更新)
        /// </summary>
        public async FTask OnLoad(AssemblyManifest assemblyManifest)
        {
            if (assemblyManifest.Assembly.FullName == _AssemblyFullName)
            {
                string configfile = Path.Combine(AppContext.BaseDirectory, _JwtConfig);
                try
                {
                    string json = await FileHelper.GetTextByRelativePath(configfile);
                    var config = json.Deserialize<JwtConfig>();

                    if (string.IsNullOrWhiteSpace(config.SecretKey))
                        throw new($"JWT config conatains a null or white-space secret key.");
                  
                    if (_config == null)
                    {
                        Log.Info($"Inited {_JwtConfig}.");
                        UpdateConfig(config);
                    }
                    else if (_config != null && config.SecretKey != _config.SecretKey)
                    {
                        Log.Warning($"JWT config`s secret key gonna update. The old \"{_config.SecretKey}\" will be invalid.");
                        UpdateConfig(config);
                    }
                }
                catch (Exception ex) {
                    throw new($"Failed To Load {configfile}, Mag:\n{ex}");
                }
            }
        }

        /// <summary>
        /// 程序集卸载时调用
        /// </summary>
        public async FTask OnUnload(AssemblyManifest assemblyManifest)
        {
            await FTask.CompletedTask;
        }

        /// <summary>
        /// 默认令牌有效期，单位：分钟
        /// </summary>
        public static readonly int TokenDefaultDurationMinutes = 25;

        /// <summary>
        /// 申请一个JWT。
        /// </summary>
        /// <param name="uniqueId">用户唯一标识符</param>
        /// <returns>JWT 字符串</returns>
        public static string RequestToken(string uniqueId) { return RequestToken(uniqueId);}

        /// <summary>
        /// 申请一个JWT。
        /// </summary>
        /// <param name="uniqueId">用户唯一标识符</param>
        /// <param name="additionalClaims">额外自定义 Claim</param>
        /// <param name="duration">令牌有效持续时间</param>
        /// <returns>JWT 字符串</returns>
        public static string RequestToken(
          string uniqueId,
          Claim[]? additionalClaims = null,
          TimeSpan duration = default
        )
        {
            if (string.IsNullOrWhiteSpace(uniqueId))
                throw new ArgumentNullException(nameof(uniqueId));

            // 设置默认有效期
            if (duration == default)
                duration = TimeSpan.FromMinutes(TokenDefaultDurationMinutes);

            var expires = DateTime.UtcNow.Add(duration);

            var claims = new List<Claim>
            {
                // 用户唯一标识符
                new Claim(JwtRegisteredClaimNames.Sub, uniqueId),
                // Token 唯一标识
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                // Token 发行时间
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                                                       ClaimValueTypes.Integer64)  
            };

            if (additionalClaims != null)
                claims.AddRange(additionalClaims);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                Issuer = _config.Issuer,
                Audience = _config.Audience,
                SigningCredentials = _credentials
            };

            var jwt = new JwtSecurityToken(
               issuer: _config.Issuer,
               audience: _config.Audience,
               claims: claims,
               notBefore: DateTime.UtcNow,
               expires: expires,
               signingCredentials: _credentials
           );

            return _tokenHandler.WriteToken(jwt);
        }

        /// <summary>
        /// 刷新过期令牌
        /// </summary>
        /// <param name="oldToken">用户唯一标识符</param>
        /// <returns></returns>
        public static string RefreshToken(
         SecurityToken oldToken
        )
        {
            //TODO 实现刷新逻辑，例如检查旧令牌的有效性，生成新令牌等

            return "";
        }

        /// <summary>
        /// 验证 JWT 并返回 ClaimsPrincipal，如果验证失败会抛出异常
        /// </summary>
        public static ClaimsPrincipal ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentNullException(nameof(token));

            ClaimsPrincipal principal = _tokenHandler.ValidateToken(
                token, 
                _tokenValidationParameters, 
                out SecurityToken validatedToken);

            if (validatedToken is not JwtSecurityToken jwt ||
                    !jwt.Header.Alg.Equals( _algorithms, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }

        /// <summary>
        /// 简单验证 token 是否有效, 不会抛异常。
        /// 真实检测环境请使用<see cref="ValidateToken"/>。
        /// </summary>
        public static bool IsTokenValid(string token)
        {
            try
            {
                ValidateToken(token);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
#endif