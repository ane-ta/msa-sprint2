1. dockerfile 
- двухступенчатый для минимального размера итогового образа.
- открывает порт 8080
2. helm-chart/deployment
- использует значения values
- livenessProbe, readinessProbe по адресу из values
3. helm-chart/values
- ресурсы
- ENABLE_FEATURE_X как переменная env
4. helm-chart/values.dev
- 1 реплика - для экономии ресурсов машины разработчика
- образ никогда не качаем
- фича включена - для тестирования
5. helm-chart/values.prod
- 3 реплики - для повышения надежности
- образ качаем
- фича выключена