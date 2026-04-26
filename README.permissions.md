# Matriz de Permissões da API

[Voltar para o README principal](./README.md)

Este documento lista os endpoints expostos pela WebApi e resume as permissões exigidas por controller. As regras abaixo refletem os atributos `Authorize`, `AllowAnonymous`, roles e políticas configurados nos controllers.

## Convenções

| Marcador | Significado |
|---|---|
| Permitido | A role pode acessar o endpoint |
| Negado | A role não pode acessar o endpoint |
| Público | Não exige autenticação |
| `ActiveUser` | Exige usuário autenticado e ativo |

## Índice

- [Resumo por Role](#resumo-por-role)
- [Auth](#auth)
- [Users](#users)
- [Products](#products)
- [Categories](#categories)
- [Carts](#carts)
- [Sales](#sales)
- [Branches](#branches)
- [Customers](#customers)
- [Inventories](#inventories)

## Resumo por Role

| Role | Principais permissões |
|---|---|
| `Admin` | Gestão de usuários, consulta/criação/edição de filiais e exclusão de filiais |
| `Manager` | Gestão operacional de produtos, categorias, vendas, filiais, clientes e inventário |
| `Customer` | Catálogo, carrinho, criação/consulta de vendas e avaliação de produtos |

> Observação: todas as permissões autenticadas dependem da política `ActiveUser`.

## Auth

| Método | Endpoint | Admin | Manager | Customer | Política |
|---|---|---|---|---|---|
| `POST` | `/api/auth/login` | Público | Público | Público | `AllowAnonymous` |

## Users

| Método | Endpoint | Admin | Manager | Customer | Política / Observação |
|---|---|---|---|---|---|
| `GET` | `/api/users` | Permitido | Permitido | Negado | `ActiveUser` |
| `POST` | `/api/users` | Permitido | Permitido | Negado | `ActiveUser` |
| `GET` | `/api/users/{id}` | Permitido | Permitido | Negado | `ActiveUser` |
| `PUT` | `/api/users/{id}` | Permitido | Permitido | Negado | `ActiveUser` |
| `PATCH` | `/api/users/{id}/password` | Permitido | Permitido | Permitido | `ActiveUser`; permite alterar apenas a própria senha |
| `DELETE` | `/api/users/{id}` | Permitido | Permitido | Negado | `ActiveUser` |

## Products

| Método | Endpoint | Admin | Manager | Customer | Política |
|---|---|---|---|---|---|
| `GET` | `/api/products/categories` | Negado | Negado | Permitido | `ActiveUser` |
| `GET` | `/api/products` | Negado | Negado | Permitido | `ActiveUser` |
| `GET` | `/api/products/{id}` | Negado | Negado | Permitido | `ActiveUser` |
| `POST` | `/api/products` | Negado | Permitido | Negado | `ActiveUser` |
| `POST` | `/api/products/{id}/ratings` | Negado | Negado | Permitido | `ActiveUser` |
| `PUT` | `/api/products/{id}` | Negado | Permitido | Negado | `ActiveUser` |
| `DELETE` | `/api/products/{id}` | Negado | Permitido | Negado | `ActiveUser` |

## Categories

| Método | Endpoint | Admin | Manager | Customer | Política |
|---|---|---|---|---|---|
| `GET` | `/api/categories/{id}/products` | Negado | Permitido | Negado | `ActiveUser` |

## Carts

| Método | Endpoint | Admin | Manager | Customer | Política |
|---|---|---|---|---|---|
| `GET` | `/api/carts` | Negado | Negado | Permitido | `ActiveUser` |
| `GET` | `/api/carts/{id}` | Negado | Negado | Permitido | `ActiveUser` |
| `POST` | `/api/carts` | Negado | Negado | Permitido | `ActiveUser` |
| `PUT` | `/api/carts/{id}` | Negado | Negado | Permitido | `ActiveUser` |
| `DELETE` | `/api/carts/{id}` | Negado | Negado | Permitido | `ActiveUser` |

## Sales

| Método | Endpoint | Admin | Manager | Customer | Política / Observação |
|---|---|---|---|---|---|
| `POST` | `/api/sales` | Negado | Negado | Permitido | `ActiveUser`; processamento assíncrono |
| `GET` | `/api/sales` | Negado | Negado | Permitido | `ActiveUser` |
| `GET` | `/api/sales/{id}` | Negado | Negado | Permitido | `ActiveUser` |
| `GET` | `/api/sales/messages/{correlationId}` | Negado | Negado | Permitido | `ActiveUser`; consulta status assíncrono |
| `PUT` | `/api/sales/{id}` | Negado | Permitido | Negado | `ActiveUser`; processamento assíncrono |
| `DELETE` | `/api/sales/{id}` | Negado | Permitido | Negado | `ActiveUser`; processamento assíncrono |
| `POST` | `/api/sales/{id}/cancel` | Negado | Permitido | Negado | `ActiveUser`; processamento assíncrono |

## Branches

| Método | Endpoint | Admin | Manager | Customer | Política |
|---|---|---|---|---|---|
| `GET` | `/api/branches` | Permitido | Permitido | Negado | `ActiveUser` |
| `GET` | `/api/branches/{id}` | Permitido | Permitido | Negado | `ActiveUser` |
| `POST` | `/api/branches` | Permitido | Permitido | Negado | `ActiveUser` |
| `PUT` | `/api/branches/{id}` | Permitido | Permitido | Negado | `ActiveUser` |
| `DELETE` | `/api/branches/{id}` | Permitido | Negado | Negado | `ActiveUser` |

## Customers

| Método | Endpoint | Admin | Manager | Customer | Política |
|---|---|---|---|---|---|
| `GET` | `/api/customers` | Negado | Permitido | Negado | `ActiveUser` |
| `GET` | `/api/customers/{id}` | Negado | Permitido | Negado | `ActiveUser` |
| `POST` | `/api/customers` | Negado | Permitido | Negado | `ActiveUser` |
| `PUT` | `/api/customers/{id}` | Negado | Permitido | Negado | `ActiveUser` |
| `DELETE` | `/api/customers/{id}` | Negado | Permitido | Negado | `ActiveUser` |

## Inventories

| Método | Endpoint | Admin | Manager | Customer | Política |
|---|---|---|---|---|---|
| `GET` | `/api/inventories` | Negado | Permitido | Negado | `ActiveUser` |
| `GET` | `/api/inventories/{id}` | Negado | Permitido | Negado | `ActiveUser` |
| `POST` | `/api/inventories` | Negado | Permitido | Negado | `ActiveUser` |
| `PUT` | `/api/inventories/{id}` | Negado | Permitido | Negado | `ActiveUser` |
| `DELETE` | `/api/inventories/{id}` | Negado | Permitido | Negado | `ActiveUser` |

---

[Voltar para o README principal](./README.md)
