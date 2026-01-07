import { ApolloServer } from '@apollo/server';
import { startStandaloneServer } from '@apollo/server/standalone';
import { buildSubgraphSchema } from '@apollo/subgraph';
import gql from 'graphql-tag';

const typeDefs = gql`
extend schema
  @link(url: "https://specs.apollo.dev/federation/v2.7",
        import: ["@key", "@shareable", "@inaccessible", "@override", "@requires", "@external"])

  type PromoCode @key(fields: "promoCode") {
    promoCode: String!
    discountPercent: Int!
    isVip: Boolean!

    description: String
    expiresAt: String
  }

  type DiscountInfo {
    promoApplied: Boolean
    description: String
  }

  extend type Booking @key(fields: "id") {
    id: ID! @external
    promoCode: String @external
    
    discountPercent: Int! @override(from: "booking") @requires(fields: "promoCode")
    discountInfo: DiscountInfo @requires(fields: "promoCode")
  }
  type Query {
       promoByCode(code: String!): PromoCode
  }

`;

const mockPromos = [
  { promoCode: "SUMMER_10", discountPercent: 10, isVip: "true", description: "Summer Sale 10% off" },
  { promoCode: "WINTER_20", discountPercent: 20, isVip: "false", description: "Winter Sale 20% off" },
];

function findPromocodeByCode(code) {
  return mockPromos.find(p => p.promoCode === code);
}
function validatePromocode(code) {
  return mockPromos.find(p => p.promoCode === code);
}

const resolvers = {
  Query: {
    promoByCode: async (_, { code }) => {
        return findPromocodeByCode(code);
    },
  },
  // PromoCode: {
  //   __resolveReference: ({ promoCode }) => {
  //     return promocodes.find(p => p.promoCode === promoCode);
  //   }
  // },
  Booking:{
    discountPercent: (booking) =>{
      const promo = validatePromocode(booking.promoCode);
      console.log("promo found for code", booking.promoCode, promo);
      if(promo){
        return promo.discountPercent;
      }

      return 0;
    },
    discountInfo: (booking) => {
      const promo = validatePromocode(booking.promoCode);
      console.log("validated promo found for code", booking.promoCode, promo);

      if(promo){
        return {
          promoApplied: true,
          description: promo.description,
        }
      }
      return {
        promoApplied: false,
      }
    }
  }
};

const server = new ApolloServer({
  schema: buildSubgraphSchema([{ typeDefs, resolvers }]),
});

startStandaloneServer(server, {
  listen: { port: 4003 },
  context: async ({ req }) => ({ req }),
}).then(() => {
  console.log('✅ Promo subgraph ready at http://localhost:4003/');
});
