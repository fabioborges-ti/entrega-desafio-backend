# Defesas Tecnicas do Projeto

> Documento complementar ao README principal para registrar, de forma incremental, as decisoes tecnicas tomadas durante o desenvolvimento da API.

[Voltar para o README principal](./README.md)

## Nota de interpretacao do desafio

Este documento nao tem o objetivo de contrariar a documentacao original do desafio, mas registrar as decisoes tecnicas tomadas nos pontos em que o contrato permitia interpretacao ou apresentava riscos de consistencia.

Sempre que a especificacao indicava dados de resposta como parte de um recurso, mas nao deixava claro se esses dados deveriam ser fornecidos pelo cliente ou calculados pelo dominio, a solucao privilegiou a consistencia da regra de negocio.

Por esse motivo, valores como totais de venda, descontos, precos aplicados e agregados de avaliacao nao foram tratados como entradas autoritativas do usuario. Eles sao calculados pela aplicacao a partir de dados controlados, como catalogo, carrinho, avaliacoes persistidas e politicas de dominio.

Essa abordagem busca manter aderencia funcional ao desafio, preservando os dados esperados nas respostas da API, mas evitando payloads que permitam manipulacao manual de informacoes derivadas.

## Caso de Uso Vendas

**Contexto**

O enunciado do desafio define que a API de vendas deve ser capaz de informar dados completos da venda, incluindo numero da venda, data, cliente, filial, produtos, quantidades, precos unitarios, descontos, total por item, total geral e status de cancelamento.

Entretanto, diferente de outros dominios do projeto, o caso de uso de vendas nao apresenta um contrato explicito de payload para operacoes como `POST` e `PUT`. Diante disso, a modelagem do contrato de criacao da venda ficou sob responsabilidade do desenvolvedor, respeitando os principios de DDD, consistencia de dominio e as entidades ja existentes no sistema.

**Decisao**

A criacao de uma venda foi modelada a partir de um carrinho previamente criado, em vez de permitir que o cliente da API informe manualmente produtos, precos unitarios, descontos e totais no payload da venda.

Assim, o fluxo adotado e:

1. O usuario seleciona produtos do catalogo e informa quantidades no carrinho.
2. O sistema valida disponibilidade, existencia dos produtos e regras de quantidade.
3. A criacao da venda recebe referencias para cliente, filial e carrinho.
4. Os itens da venda sao derivados das linhas do carrinho.
5. Os precos unitarios sao obtidos do catalogo de produtos.
6. Os descontos sao calculados pela politica de dominio.
7. O total por item e o total da venda sao calculados pela propria aplicacao.

**Defesa tecnica**

Essa decisao evita que dados sensiveis de negocio sejam definidos pelo consumidor da API. Preco unitario, desconto e total nao devem ser tratados como entrada confiavel, pois representam regras e calculos controlados pelo dominio.

Permitir que o cliente envie manualmente `unitPrice`, `discount` ou `totalAmount` abriria margem para inconsistencias, manipulacao de valores e divergencia entre catalogo, carrinho, inventario e venda. Ao derivar a venda a partir do carrinho e do catalogo, a aplicacao mantem uma fonte de verdade clara para cada informacao:

- o catalogo define o produto e o preco vigente;
- o carrinho define intencao de compra e quantidades;
- o dominio de vendas calcula descontos e totais;
- a venda persiste o resultado final como registro transacional.

Essa abordagem tambem esta alinhada ao uso de **External Identities** citado no enunciado: a venda referencia entidades externas por identificadores, como `CustomerId`, `BranchId`, `CartId` e `ProductId`, sem duplicar a responsabilidade de criacao desses dominios dentro do payload de venda.

**Decisao complementar: eventos de vendas**

Os eventos `SaleCreated`, `SaleModified`, `SaleCancelled` e `ItemCancelled` foram interpretados como eventos de dominio relacionados ao ciclo de vida da venda e de seus itens, nao como comandos livres para criacao arbitraria de dados.

Essa decisao reforca que a venda precisa nascer de uma origem operacional consistente. No contexto deste projeto, essa origem e o carrinho: ele representa a intencao de compra, contem os produtos selecionados a partir do catalogo e concentra as quantidades informadas pelo usuario antes da consolidacao da venda.

Dessa forma:

- `SaleCreated` representa a consolidacao de um carrinho valido em uma venda;
- `SaleModified` representa a alteracao controlada de uma venda a partir de nova referencia de carrinho, cliente, filial ou data;
- `SaleCancelled` representa o cancelamento da venda como registro transacional;
- `ItemCancelled` representa o cancelamento de um item especifico da venda, preservando a rastreabilidade do produto, quantidade, preco aplicado e desconto calculado.

**Defesa tecnica dos eventos**

A existencia desses eventos indica que a venda possui ciclo de vida proprio e deve produzir fatos de dominio rastreaveis. Isso nao combina com uma criacao aleatoria de venda, na qual o consumidor da API informaria livremente produtos, precos, descontos e totais.

Especialmente no caso de `ItemCancelled`, e necessario que exista um item de venda previamente consolidado. Esse item precisa ter origem clara, estar associado a um produto do catalogo, possuir quantidade validada, preco aplicado, desconto calculado e vinculo com uma venda. O carrinho fornece exatamente essa base antes da venda ser criada.

Portanto, a decisao de criar vendas a partir de carrinhos tambem torna os eventos mais coerentes: os eventos deixam de ser apenas notificacoes tecnicas e passam a representar mudancas reais em um fluxo de negocio auditavel.

No estado atual do projeto, os eventos `SaleCreated`, `SaleModified` e `SaleCancelled` ja estao representados como eventos publicados por logging estruturado. O evento `ItemCancelled` permanece como evolucao natural do dominio, ja que a entidade `SaleItem` possui estado de cancelamento individual por meio de `IsCancelled`.

Essa escolha permite evoluir o sistema futuramente para publicar `ItemCancelled` explicitamente sem mudar a modelagem principal da venda.

**Exemplos no projeto**

- `src/Ambev.DeveloperEvaluation.WebApi/Features/Sales/CreateSale/CreateSaleRequest.cs`
- `src/Ambev.DeveloperEvaluation.WebApi/Features/Carts/CartApiModels.cs`
- `src/Ambev.DeveloperEvaluation.Application/Sales/CreateSale/CreateSaleHandler.cs`
- `src/Ambev.DeveloperEvaluation.Application/Sales/CartSaleCommandItemMapper.cs`
- `src/Ambev.DeveloperEvaluation.Domain/Entities/Sale.cs`
- `src/Ambev.DeveloperEvaluation.Domain/Entities/SaleItem.cs`
- `src/Ambev.DeveloperEvaluation.Domain/Services/QuantityDiscountPolicy.cs`

**Trade-offs**

O fluxo exige uma etapa anterior de criacao ou atualizacao do carrinho antes da venda. Isso adiciona uma chamada a mais para o cliente da API, mas torna o processo mais consistente, auditavel e seguro.

O ganho principal e preservar a integridade do dominio: a venda deixa de depender de valores arbitrarios enviados pelo usuario e passa a ser construida a partir de dados ja validados pelo catalogo, carrinho, inventario e regras de desconto.

## Caso de Uso Produtos

**Contexto**

A documentacao original de produtos apresenta `rating` nos contratos de `POST /products` e `PUT /products/{id}`, contendo `rate` e `count` como parte do payload de escrita do produto.

Embora esse formato apareca no contrato de referencia, ele mistura duas responsabilidades diferentes: os dados cadastrais do produto e a agregacao das avaliacoes feitas por usuarios. Em termos de dominio, `rating.rate` e `rating.count` nao representam atributos que um usuario ou gestor deveria informar manualmente ao criar ou atualizar um produto; eles representam um resultado calculado a partir de avaliacoes reais.

**Decisao**

A criacao e atualizacao de produtos foram modeladas sem aceitar `rating` no payload principal. Os contratos de `POST` e `PUT` tratam apenas os dados cadastrais do produto:

1. titulo;
2. preco;
3. descricao;
4. categoria;
5. imagem.

Para avaliacoes, foi criado um endpoint especifico: `POST /api/products/{id}/ratings`. Esse endpoint recebe apenas a nota informada pelo usuario autenticado, enquanto a aplicacao associa a avaliacao ao produto e ao usuario responsavel.

**Defesa tecnica**

Essa decisao evita que o consumidor da API defina manualmente valores agregados que deveriam ser controlados pelo sistema. Permitir que `rating.rate` e `rating.count` fossem enviados em `POST` ou `PUT` abriria margem para inconsistencias, como um produto ser criado com media `5.0` e contagem `999` sem que existam avaliacoes correspondentes.

O modelo adotado preserva uma fonte de verdade clara:

- o cadastro do produto define informacoes comerciais e descritivas;
- cada avaliacao individual registra a nota dada por um usuario;
- os metodos de leitura calculam e expõem `rating.rate` como media das avaliacoes;
- os metodos de leitura calculam e expõem `rating.count` como quantidade total de avaliacoes.

Dessa forma, `rating` passa a ser uma informacao derivada, nao uma entrada autoritativa. Isso torna o comportamento mais auditavel, evita manipulacao manual de indicadores e deixa a API mais coerente com a ideia de que avaliacoes pertencem ao ciclo de interacao do usuario com o produto.

**Decisao complementar: avaliacoes como recurso proprio**

O endpoint dedicado de avaliacao tambem torna o contrato mais expressivo. Avaliar um produto nao e a mesma operacao que criar ou editar o cadastro do produto. Criar ou editar um produto e uma acao administrativa; avaliar um produto e uma acao de usuario consumidor.

Separar essas operacoes permite aplicar regras de autorizacao diferentes, registrar o usuario responsavel pela avaliacao e evoluir o dominio sem alterar o contrato cadastral do produto. Por exemplo, o sistema pode permitir multiplas avaliacoes por usuario para capturar historico ou, em uma evolucao futura, restringir para uma avaliacao ativa por usuario e produto.

No estado atual do projeto, as avaliacoes sao persistidas como registros proprios e os valores agregados sao calculados a partir desses registros. Isso permite que os metodos `GET` retornem o formato esperado pelo contrato, mas sem confiar em dados agregados enviados manualmente pelo cliente.

**Exemplos no projeto**

- `src/Ambev.DeveloperEvaluation.WebApi/Features/Products/ProductApiModels.cs`
- `src/Ambev.DeveloperEvaluation.WebApi/Features/Products/ProductsController.cs`
- `src/Ambev.DeveloperEvaluation.Application/Products/RateProduct/RateProductHandler.cs`
- `src/Ambev.DeveloperEvaluation.Application/Products/ProductDto.cs`
- `src/Ambev.DeveloperEvaluation.Domain/Entities/ProductUserRating.cs`
- `src/Ambev.DeveloperEvaluation.ORM/Repositories/ProductRatingRepository.cs`
- `src/Ambev.DeveloperEvaluation.ORM/Mapping/ProductUserRatingConfiguration.cs`

**Trade-offs**

Essa abordagem adiciona um endpoint especifico para avaliacao, em vez de concentrar tudo em `POST` e `PUT` de produtos. O custo e pequeno e melhora a clareza do contrato, pois cada operacao passa a representar uma intencao de negocio distinta.

O ganho principal e manter integridade e rastreabilidade: a media e a contagem de avaliacoes deixam de ser valores arbitrarios e passam a ser resultado de interacoes reais dos usuarios com o produto.

---

## Frameworks

**Contexto**

Durante a evolucao do projeto, foi avaliada a possibilidade de utilizar frameworks de mensageria, como Rebus, em conjunto com o RabbitMQ ja implementado.

O Rebus e um framework valido para abstracao de mensageria em .NET. Ele pode usar RabbitMQ como transporte e oferece recursos prontos para handlers, retries, roteamento, publicacao de mensagens e tratamento de erros. Entretanto, o projeto ja possui uma integracao RabbitMQ explicita para o fluxo assincrono de vendas, incluindo publisher, consumer em background, topologia de filas, exchange, routing keys, dead-letter e rastreamento por `correlationId`.

**Decisao**

Optou-se por manter a implementacao direta com RabbitMQ neste momento, em vez de adicionar Rebus sobre a infraestrutura existente.

Essa decisao nao descarta o uso de Rebus como evolucao futura. Ela apenas evita introduzir uma segunda camada de abstracao enquanto a necessidade atual ja esta atendida por uma implementacao simples, clara e alinhada ao escopo do desafio.

**Defesa tecnica**

Adicionar Rebus agora exigiria reavaliar toda a fronteira de mensageria: publicadores, consumidores, topologia, retries, dead-letter, logs, contratos de mensagens e rastreamento de status. Usar Rebus em paralelo com o consumer manual atual poderia gerar concorrencia pelas mesmas filas, duplicidade de processamento ou dificuldade de diagnostico.

No estado atual, a implementacao direta com RabbitMQ torna o fluxo mais transparente para avaliacao. E possivel observar explicitamente:

- qual exchange e utilizada;
- quais routing keys representam cada operacao;
- quais filas processam criacao, atualizacao, cancelamento e delecao de vendas;
- como o `correlationId` e retornado para a API;
- como o consumer interpreta cada mensagem;
- como retry e dead-letter sao tratados.

Para um desafio tecnico, essa clareza tem valor. Ela demonstra conhecimento do broker e evita esconder decisoes importantes atras de uma abstracao mais poderosa do que o necessario.

**Evolucao futura**

O Rebus faria sentido em uma etapa posterior se o projeto passasse a ter mais fluxos assincronos, mais tipos de mensagens, handlers distribuidos, necessidade de padronizar retries ou desejo de reduzir codigo manual de infraestrutura.

Nesse caso, a migracao deveria ser feita de forma planejada:

1. manter RabbitMQ como transporte;
2. substituir gradualmente publishers e consumers manuais por mensagens e handlers Rebus;
3. preservar os contratos HTTP ja existentes;
4. manter o retorno `202 Accepted` com `correlationId`;
5. definir como Rebus trataria retries, dead-letter e observabilidade;
6. evitar que Rebus e consumers manuais processem a mesma fila simultaneamente.

**Exemplos no projeto**

- `src/Ambev.DeveloperEvaluation.WebApi/Messaging/Sales/RabbitMqSaleCommandPublisher.cs`
- `src/Ambev.DeveloperEvaluation.WebApi/Messaging/Sales/SaleCommandConsumerBackgroundService.cs`
- `src/Ambev.DeveloperEvaluation.WebApi/Messaging/Sales/SalesRabbitMqTopology.cs`
- `src/Ambev.DeveloperEvaluation.WebApi/Messaging/Sales/SalesMessageStatus.cs`
- `README.sales-async.md`

**Trade-offs**

Manter RabbitMQ direto exige mais codigo de infraestrutura proprio. Em contrapartida, reduz dependencias, deixa o comportamento explicito e evita complexidade adicional em um fluxo que ja esta funcional.

Adotar Rebus poderia simplificar handlers e politicas de mensageria no longo prazo, mas adicionaria uma camada conceitual nova e exigiria migracao cuidadosa. Por isso, a decisao atual privilegia simplicidade, rastreabilidade e aderencia ao escopo.

---

## Apoio visual para avaliacao

Para complementar a leitura das decisoes tecnicas descritas neste documento, foi disponibilizado o arquivo `images/Desafio-MER.png`.

Esse material apresenta uma visao visual do modelo de entidades e relacionamentos adotado na solucao, auxiliando a compreensao da estrutura de dominio, das dependencias entre agregados e das escolhas feitas para manter consistencia entre vendas, carrinhos, produtos, clientes, filiais, inventario e avaliacoes.

A intencao e oferecer aos avaliadores um recurso adicional de analise, facilitando a correlacao entre a modelagem implementada, os fluxos expostos pela API e as defesas tecnicas registradas neste documento.

---

