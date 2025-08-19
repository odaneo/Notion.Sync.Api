TODO:
~~1. 整理统一优化AWS Secrets Manager，寻找更加优雅的方法~~
~~2. Hangfire优化，持久化~~
~~3. 日志优化~~
4. Docker化以及cicd，github action
4.1 本地环境调试aws，抛弃if dev代码
5. 上Scaling Group，完成零停机滚动更新，最后丢弃ALB
6. CloudFront拦截非前端流量，以及CDN
7. DB优化，加redis，hangfire任务完成后，刷新redis
