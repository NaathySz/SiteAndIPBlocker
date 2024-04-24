# SiteAndIPBlocker
A chat plugin that blocks websites and IP addresses, with a configurable whitelist for allowed sites and IPs.

## Available colors
```
{default}
{white}
{darkred}
{green}
{lightyellow}
{lightblue}
{olive}
{lime}
{red}
{lightpurple}
{purple}
{grey}
{yellow}
{gold}
{silver}
{blue}
{darkblue}
{bluegrey}
{magenta}
{lightred}
{orange}
```

## Config example
The configuration file will be automatically generated in `csgo/addons/counterstrikesharp/configs/plugins/IPAndSiteBlocker/IPAndSiteBlocker.json`
```
{
  "whitelist": [
    "https://example.com",
    "192.168.0.1"
    ],
  "block_message": " {darkred}Blocked: Sending IP addresses or websites is not allowed.",
  "ConfigVersion": 1
}
```
