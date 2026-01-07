import { ApolloServer } from '@apollo/server';
import { startStandaloneServer } from '@apollo/server/standalone';
import { ApolloGateway, IntrospectAndCompose, RemoteGraphQLDataSource } from '@apollo/gateway';

class AuthenticatedDataSource extends RemoteGraphQLDataSource {
  willSendRequest({ request, context }) {
      if (context.headers && context.headers['userid']) {
        request.http.headers.set('userid', context.headers['userid']);
      }
  }
}
const gateway = new ApolloGateway({
  supergraphSdl: new IntrospectAndCompose({
    subgraphs: [
      { name: 'booking', url: 'http://booking-subgraph:4001/graphql' }, 
      { name: 'hotel', url: 'http://hotel-subgraph:4002/graphql' },
      { name: 'promo', url: 'http://promo-subgraph:4003/graphql' }   
    ],
  }),
  buildService({ name, url }) {
    return new AuthenticatedDataSource({ url });
  }
});

const server = new ApolloServer({ gateway, subscriptions: false });

startStandaloneServer(server, {
  listen: { port: 4000 },
  context: async ({ req }) => ({ headers: req.headers }) // headers пробрасываются
}).then(({ url }) => {
  console.log(`🚀 Gateway ready at ${url}`);
});
