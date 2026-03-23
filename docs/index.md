# TaskManager API

Документация серверной части системы управления задачами.

## Контроллеры

| Контроллер | Описание |
|---|---|
| [AuthController](api/TaskManager.API.Controllers.AuthController.html) | Регистрация, вход, обновление и отзыв токенов |
| [ProjectsController](api/TaskManager.API.Controllers.ProjectsController.html) | CRUD-операции над проектами |
| [TasksController](api/TaskManager.API.Controllers.TasksController.html) | Управление задачами с фильтрацией |
| [UsersController](api/TaskManager.API.Controllers.UsersController.html) | Управление пользователями |

## Быстрый старт

Все эндпоинты (кроме `auth/register` и `auth/login`) требуют JWT-токен:

Authorization: Bearer <ваш_токен>

Получить токен: `POST /api/auth/login`